using System.Text.RegularExpressions;
using Chrono.Core.GitInfo;
using Chrono.Core.Helpers;
using Huxy;
using LibGit2Sharp;
using NLog;
using Version = System.Version;

namespace Chrono.Core;

public enum VersionComponent
{
    Major,
    Minor,
    Patch,
    Build
}

public class VersionInfo
{
    #region Properties

    #region VersionInfo

    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public int Build { get; private set; }

    #endregion

    #region GitInfo

    #endregion

    public VersionFile File { get; }

    public IGitInfoProvider GitInfoProvider { get; internal set; }
    public BranchConfigWithFallback CurrentBranchConfig { get; private set; }

    #endregion

    #region Members

    private Dictionary<string, BranchConfig> SearchBranches { get; set; }
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly string _versionPath;
    private string _parsedVersion = "";

    #endregion

    internal VersionInfo(string path, IGitInfoProvider gitInfoProvider, bool allowDirtyRepo = false,
        bool useEnvVars = false)
    {
        _versionPath = path;
        GitInfoProvider = gitInfoProvider;

        var fileResult = VersionFile.From(_versionPath);
        if (!fileResult.Success)
        {
            if (fileResult.Exception is not null)
            {
                throw fileResult.Exception;
            }

            throw new Exception(fileResult.Message);
        }

        File = fileResult.Data;
        if (Version.TryParse(File.Version, out var version))
        {
            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build;
            Build = version.Revision;
        }

        var gitRes = GitInfoProvider.LoadGitInfo(allowDirtyRepo, File.Default.DirtyRepo);
        if (!gitRes)
        {
            if (gitRes.Exception is not null)
            {
                throw gitRes.Exception;
            }

            throw new Exception(gitRes.Message);
        }

        SearchBranches = new Dictionary<string, BranchConfig>(File.Branches);
        SearchBranches.Add("Default_Release_Config", File.Default.Release);

        var currentBranchResult = GetConfigForCurrentBranch();
        if (!currentBranchResult)
        {
            if (currentBranchResult.Exception is not null)
            {
                throw currentBranchResult.Exception;
            }

            throw new Exception(currentBranchResult.Message);
        }

        CurrentBranchConfig = new BranchConfigWithFallback(File.Default, currentBranchResult.Data);
    }

    /// <summary>
    /// A catch-all method that attempts to resolve and parse a <see cref="VersionInfo"/> based on the defaults.
    /// </summary>
    /// <returns>A result containing the <see cref="VersionInfo"/>.</returns>
    public static Result<VersionInfo> Get(bool allowDirtyRepo = false, bool useEnvVars = false, string rootPath = "",
        string targetVersionFile = "")
    {
        var gitSearchDirectory = string.IsNullOrEmpty(rootPath) ? Environment.CurrentDirectory : rootPath;
        var targetVersionFileSearchPath =
            string.IsNullOrEmpty(targetVersionFile) ? gitSearchDirectory : targetVersionFile;
        var gitDirectory = Repository.Discover(gitSearchDirectory);
        if (string.IsNullOrEmpty(gitDirectory))
        {
            return Result.Fail<VersionInfo>($"Chrono GitVersioning: No git directory found at {gitSearchDirectory}");
        }

        var repoRootResult = ResolveRepoRoot(gitDirectory);
        if (!repoRootResult)
        {
            return Result.Fail<VersionInfo>(repoRootResult);
        }

        var versionFileFoundResult = DirectoryHelper.Find(targetVersionFileSearchPath, repoRootResult.Data);

        if (!versionFileFoundResult)
        {
            return Result.Fail<VersionInfo>(versionFileFoundResult);
        }

        try
        {
            var gitInfoProvider = useEnvVars ? GitInfo.GitInfo.Get() : new GitRepoProvider();
            return Result.Ok(new VersionInfo(versionFileFoundResult.Data, gitInfoProvider, allowDirtyRepo, useEnvVars));
        }
        catch (Exception e)
        {
            return Result.Fail<VersionInfo>(e);
        }
    }

    /// <summary>
    /// Resolves the working directory for a discovered git directory. For a normal repository this is
    /// derived purely from the path string (the parent of ".git"), without opening a
    /// <see cref="Repository"/> - this keeps the common case exempt from LibGit2Sharp's repository
    /// ownership validation, which rejects opening a repository whose directory isn't owned by the
    /// current user (relevant in some containerized CI setups).
    /// Anything that doesn't have that shape - linked worktrees, submodules, unusually named bare repositories - falls back
    /// to actually opening the repository, which is acceptable to resolve those correctly.
    /// </summary>
    private static Result<string> ResolveRepoRoot(string gitDirectory)
    {
        var trimmed = gitDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Ok(Path.GetDirectoryName(trimmed));
        }

        using var repo = new Repository(gitDirectory);
        var workingDirectory = repo.Info.WorkingDirectory;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            return Result.Fail<string>(
                $"Chrono GitVersioning: '{gitDirectory}' has no working directory (bare repository?)");
        }

        return Result.Ok(workingDirectory);
    }

    #region VersionMethods

    /// <summary>
    /// Retrieves the current branch config and returns the parsed version for the current branch.
    /// </summary>
    /// <param name="schema">The version schema.</param>
    /// <returns>A result containing the parsed version string.</returns>
    public Result<string> GetVersion(string schema = "")
    {
        try
        {
            if (string.IsNullOrEmpty(schema))
            {
                _logger.Trace("No custom schema provided. Using schema from branch config");
                schema = CurrentBranchConfig.VersionSchema;
                _logger.Trace($"Schema: {schema}");
            }

            var schemaWithValues = ParseSchema(schema);
            var validated = schemaWithValues.Replace('/', '-');
            _parsedVersion = validated;
            return Result.Ok(validated);
        }
        catch (Exception e)
        {
            _parsedVersion = "";
            return Result.Fail<string>(e.ToString());
        }
    }

    /// <summary>
    /// Gets the numeric part of the parsed version ignoring any suffixes.
    /// </summary>
    /// <returns>A result containing the numeric version string.</returns>
    public Result<string> GetNumericVersion()
    {
        if (_parsedVersion == "")
        {
            GetVersion();
        }

        var numericVersionMatch = RegexPatterns.NumericVersionOnlyRegex.Match(_parsedVersion);
        if (numericVersionMatch.Success)
        {
            return Result.Ok(numericVersionMatch.Value);
        }

        _parsedVersion = "";
        return Result.Fail<string>("Could not parse numeric version");
    }

    /// <summary>
    /// Sets the provided version as the new one and saves the version.yml.
    /// </summary>
    /// <param name="newVersion">The new version string.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result SetVersion(string newVersion = "")
    {
        var setFromString = !string.IsNullOrEmpty(newVersion);
        _parsedVersion = "";
        var newVersionMatch = RegexPatterns.ValidVersionRegex.Match(newVersion ?? string.Empty);
        if (setFromString)
        {
            if (!newVersionMatch.Success)
            {
                return Result.Fail($"{newVersion} is not a valid version!");
            }
        }

        var fileVersionMatch = RegexPatterns.ValidVersionRegex.Match(File.Version);


        if (!fileVersionMatch.Success)
        {
            _logger.Trace($"Trace: Version present in version.yml was not correct -> '{File.Version}'");
        }

        if (setFromString)
        {
            Major = int.TryParse(newVersionMatch.Groups[1].Value, out var major) ? major : -1;
            Minor = int.TryParse(newVersionMatch.Groups[2].Value, out var minor) ? minor : -1;
            Patch = int.TryParse(newVersionMatch.Groups[3].Value, out var patch) ? patch : -1;
            Build = int.TryParse(newVersionMatch.Groups[4].Value, out var build) ? build : -1;
        }

        if (Major == -1 || Minor == -1)
        {
            return Result.Fail("Some went wrong while parsing major and minor values");
        }

        if (Patch == -1)
        {
            File.Version = $"{Major}.{Minor}";
        }
        else if (Build == -1)
        {
            File.Version = $"{Major}.{Minor}.{Patch}";
        }
        else
        {
            File.Version = $"{Major}.{Minor}.{Patch}.{Build}";
        }

        var saveResult = File.UpdateVersionInFile(_versionPath);
        return !saveResult ? saveResult : Result.Ok();
    }

    /// <summary>
    /// Bumps the version based on the specified component.
    /// </summary>
    /// <param name="component">The version component to bump.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result BumpVersion(VersionComponent component)
    {
        var parseResult = GetVersion();
        if (!parseResult)
        {
            return Result.Fail(parseResult);
        }

        var minorUsed = Minor != -1;
        var patchUsed = Patch != -1;
        var buildUsed = Build != -1;

        switch (component)
        {
            case VersionComponent.Major:
                Major++;
                Minor = minorUsed ? 0 : Minor;
                Patch = patchUsed ? 0 : Patch;
                Build = buildUsed ? 0 : Build;
                break;
            case VersionComponent.Minor:
                Minor++;
                Patch = patchUsed ? 0 : Patch;
                Build = buildUsed ? 0 : Build;
                break;
            case VersionComponent.Patch:
                Patch++;
                Build = buildUsed ? 0 : Build;
                break;
            case VersionComponent.Build:
                Build++;
                break;
            default:
                return Result.Fail("Invalid version component");
        }

        var setResult = SetVersion();
        if (!setResult)
        {
            return Result.Fail(setResult);
        }

        return Result.Ok();
    }

    #endregion

    #region SchemaMethods

    public Result<string> ResolveSchema(string schema)
    {
        try
        {
            return Result.Ok(ParseSchema(schema));
        }
        catch (Exception e)
        {
            return Result.Fail<string>($"Failed to resolve schema: {e.Message}", e);
        }
    }

    public Result<string> GetNewBranchNameFromKey(string key)
    {
        if (SearchBranches.TryGetValue(key, out var branchConfig))
        {
            var newBranchName = branchConfig.NewBranchSchema;
            if (string.IsNullOrEmpty(newBranchName))
            {
                return Result.Fail<string>($"No new branch schema configured for branch config ''{key}''");
            }

            return Result.Ok(ParseSchema(newBranchName));
        }

        return Result.Fail<string>($"No config for branch {key} found");
    }

    public Result<string> GetNewTagNameFromKey(string key)
    {
        if (SearchBranches.TryGetValue(key, out var branchConfig))
        {
            var newTagName = branchConfig.NewTagSchema;
            if (string.IsNullOrEmpty(newTagName))
            {
                return Result.Fail<string>($"No new tag schema configured for branch config ''{key}''");
            }

            return Result.Ok(ParseSchema(newTagName));
        }

        return Result.Fail<string>($"No config for branch {key} found");
    }

    #endregion

    #region Internal

    private Result<BranchConfig> GetConfigForCurrentBranch()
    {
        foreach (var branch in SearchBranches)
        {
            if (MatchRefsToConfig(branch.Value))
            {
                _logger.Trace($"{branch.Key} branch matched!");
                return Result.Ok(branch.Value);
            }
        }

        if (File.Default != null)
        {
            _logger.Trace("No branch could be matched. Using default configuration!");
            return Result.Ok<BranchConfig>(File.Default);
        }

        return Result.Fail<BranchConfig>(
            "Something went wrong while trying to get current branch config. (No branch matches | No valid default config found)");
    }

    #region ParsingMethods

    private string ParseSchema(string schema)
    {
        var schemaWithValues = schema
            .Replace("{major}", Major.ToString())
            .Replace("{minor}", Minor.ToString())
            .Replace("{patch}", Patch.ToString())
            .Replace("{build}", Build.ToString())
            .Replace("{branch}", GitInfoProvider.BranchName)
            .Replace("{prereleaseTag}", CurrentBranchConfig.PrereleaseTag)
            .Replace("{commitHash}", GitInfoProvider.CommitHash)
            .Replace("{commitShortHash}", GitInfoProvider.CommitShortHash);
        schemaWithValues = ResolveEnvironmentVariables(schemaWithValues);
        return ResolveDelimiterBlock(schemaWithValues);
    }

    private string ResolveEnvironmentVariables(string schema)
    {
        var unresolvedVariables = RegexPatterns.CurlyBracketsRegex.Matches(schema);
        foreach (Match match in unresolvedVariables)
        {
            var variableName = match.Value.Replace("{", "").Replace("}", "");
            var variableValue = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrEmpty(variableValue))
            {
                variableValue = string.Empty;
                _logger.Trace($"Environment variable '{variableName}' not found. Using empty string instead.");
            }

            schema = schema.Replace(match.Value, variableValue);
        }

        return schema;
    }

    internal static string ResolveDelimiterBlock(string input)
    {
        input = RegexPatterns.DuplicateBlocksRegex.Replace(input, "");
        input = RegexPatterns.EndBlockRegex.Replace(input, "");
        input = RegexPatterns.BlockContentRegex.Replace(input, m => m.Groups[1].Value + m.Groups[2].Value);
        return input;
    }

    #endregion

    #region GitMethods

    private bool MatchRefsToConfig(BranchConfig config)
    {
        var gitRef = !string.IsNullOrEmpty(GitInfoProvider.TagName)
            ? GitInfoProvider.TagName
            : GitInfoProvider.BranchName;

        if (config is null || !config.Match.Any()) return false;
        foreach (var matchSchema in config.Match)
        {
            var isTagSchema = matchSchema.StartsWith("tag::");
            var regexSchema = isTagSchema ? matchSchema.Substring(5) : matchSchema;

            var match = Regex.Match(gitRef, regexSchema);
            if (!match.Success) continue;

            _logger.Trace($"Match found: Regex '{regexSchema}' for branch '{gitRef}' | Branch is tag: '{isTagSchema}'");
            return true;
        }

        return false;
    }

    #endregion

    #endregion
}