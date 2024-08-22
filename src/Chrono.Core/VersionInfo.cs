using System.Text.RegularExpressions;
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
    Build,
    INVALID
}

public class VersionInfo
{
    #region Properties

    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public int Build { get; private set; }
    public string BranchName { get; private set; }
    public string[] TagNames { get; private set; }
    public string[] CombinedSearchArray => TagNames.Append(BranchName).ToArray();
    public string PrereleaseTag => CurrentBranchConfig.PrereleaseTag;
    public string CommitShortHash { get; private set; }
    public VersionFile File { get; }

    public Repository Repo { get; private set; }

    #endregion

    #region Members

    private readonly Logger _logManager = LogManager.GetCurrentClassLogger();

    private readonly string _versionPath;

    public BranchConfig CurrentBranchConfig { get; private set; }
    private string _parsedVersion = "";
    private bool _allowDirtyRepo;

    #endregion

    internal VersionInfo(string path, bool allowDirtyRepo = false)
    {
        _versionPath = path;
        File = VersionFile.From(_versionPath);
        if (Version.TryParse(File.Version, out var version))
        {
            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build;
            Build = version.Revision;
        }

        _allowDirtyRepo = allowDirtyRepo;

        LoadGitInfo();
        var currentBranchResult = GetConfigForCurrentBranch();
        CurrentBranchConfig = currentBranchResult.Success ? currentBranchResult.Data : File.Default;
    }

    /// <summary>
    /// A catch-all Method that attempts to resolve and parse a <see cref="VersionInfo"/> based on the defaults.
    /// </summary>
    /// <returns>A result containing the <see cref="VersionInfo"/>.</returns>
    public static Result<VersionInfo> Get()
    {
        var gitDirectory = Repository.Discover(Environment.CurrentDirectory);
        if (string.IsNullOrEmpty(gitDirectory))
        {
            return Result.Error<VersionInfo>("No git directory found!");
        }

        var versionFileFoundResult = VersionFile.Find(Directory.GetCurrentDirectory(), gitDirectory);

        if (!versionFileFoundResult)
        {
            return Result.Error<VersionInfo>(versionFileFoundResult);
        }

        return Result.Ok(new VersionInfo(versionFileFoundResult.Data));
    }

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
                _logManager.Trace("No schema provided. Trying to resolve schema from branch config");
                schema = CurrentBranchConfig.VersionSchema;
                _logManager.Trace($"Schema: {schema}");
            }

            var schemaWithValues = ParseSchema(schema);
            var validated = ValidateVersionString(schemaWithValues);
            _parsedVersion = validated;
            return Result.Ok(validated);
        }
        catch (Exception e)
        {
            _parsedVersion = "";
            return Result.Error<string>(e.ToString());
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
        return Result.Error<string>("Could not parse numeric version");
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
                return Result.Error<string>($"{newVersion} is not a valid version!");
            }
        }

        var fileVersionMatch = RegexPatterns.ValidVersionRegex.Match(File.Version);


        if (!fileVersionMatch.Success)
        {
            _logManager.Trace($"Info: Version present in version.yml was not correct -> '{File.Version}'");
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
            return Result.Error<string>("Some went wrong while parsing major and minor values");
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

        var saveResult = File.Save(_versionPath);
        return saveResult is IErrorResult ? saveResult : Result.Ok();
    }

    public Result<string> GetNewBranchName(bool releaseBranch = false)
    {
        var newBranchSchema = releaseBranch ? File.Default.Release.NewBranchSchema : CurrentBranchConfig.NewBranchSchema;
        if (string.IsNullOrEmpty(newBranchSchema))
        {
            return Result.Error<string>("No branch schema configured. Aborting!");
        }

        try
        {
            return Result.Ok(ParseSchema(newBranchSchema));
        }
        catch (Exception e)
        {
            return Result.Error<string>(e.ToString());
        }
    }

    public Result<string> GetNewTagName(bool releaseBranch = false)
    {
        var newBranchSchema = releaseBranch ? File.Default.Release.NewTagSchema : CurrentBranchConfig.NewTagSchema;
        if (string.IsNullOrEmpty(newBranchSchema))
        {
            return Result.Error<string>("No tag schema configured. Aborting!");
        }

        try
        {
            return Result.Ok(ParseSchema(newBranchSchema));
        }
        catch (Exception e)
        {
            return Result.Error<string>(e.ToString());
        }
    }

    /// <summary>
    /// Gets the current branch config as a result.
    /// </summary>
    /// <returns>A result containing the branch configuration.</returns>
    public Result<BranchConfig> GetConfigForCurrentBranch()
    {
        if (MatchRefsToConfig(CombinedSearchArray, File.Default.Release))
        {
            _logManager.Trace("Release branch matched!");
            return Result.Ok(File.Default.Release);
        }

        foreach (var branch in File.Branches)
        {
            if (MatchRefsToConfig(CombinedSearchArray, branch.Value))
            {
                _logManager.Trace($"{branch.Key} branch matched!");
                return Result.Ok(branch.Value);
            }
        }

        if (File.Default != null)
        {
            _logManager.Trace("No branch could be matched. Using default configuration!");
            return Result.Ok((BranchConfig)File.Default);
        }

        return Result.Error<BranchConfig>("Something went wrong while trying to get current branch");
    }

    /// <summary>
    /// Bumps the version based on the specified component.
    /// </summary>
    /// <param name="component">The version component to bump.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result BumpVersion(VersionComponent component)
    {
        var parseResult = GetVersion();
        if (parseResult is IErrorResult)
        {
            return Result.Error<string>(parseResult);
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
            case VersionComponent.INVALID:
            default:
                return Result.Error<string>("Invalid version component");
        }

        var setResult = SetVersion();
        if (setResult is IErrorResult)
        {
            return Result.Error<string>(setResult);
        }

        return Result.Ok();
    }

    public Result<string> GetNewBranchNameFromKey(string key)
    {
        if (File.Branches.TryGetValue(key, out var branchConfig))
        {
            var newBranchName = branchConfig.NewBranchSchema;
            if (string.IsNullOrEmpty(newBranchName))
            {
                return Result.Error<string>($"No branch schema configured for {key}");
            }

            return Result.Ok(ParseSchema(newBranchName));
        }

        return Result.Error<string>($"No config for branch {key} found");
    }

    public static VersionComponent VersionComponentFromString(string value)
    {
        return value switch
        {
            "major" => VersionComponent.Major,
            "minor" => VersionComponent.Minor,
            "patch" => VersionComponent.Patch,
            "build" => VersionComponent.Build,
            _ => VersionComponent.INVALID
        };
    }

    #region Internal

    private string ParseSchema(string schema)
    {
        var schemaWithValues = schema
            .Replace("{major}", Major.ToString())
            .Replace("{minor}", Minor.ToString())
            .Replace("{patch}", Patch.ToString())
            .Replace("{build}", Build.ToString())
            .Replace("{branch}", BranchName)
            .Replace("{prereleaseTag}", PrereleaseTag)
            .Replace("{commitShortHash}", CommitShortHash);
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
                _logManager.Trace($"Environment variable '{variableName}' not found. Using empty string instead.");
            }

            schema = schema.Replace(match.Value, variableValue);
        }

        return schema;
    }

    private static string ValidateVersionString(string version)
    {
        return version.Replace('/', '-');
    }

    private static string ResolveDelimiterBlock(string input)
    {
        input = RegexPatterns.DuplicateBlocksRegex.Replace(input, "");
        input = RegexPatterns.EndBlockRegex.Replace(input, "");
        input = RegexPatterns.BlockContentRegex.Replace(input, m => m.Groups[1].Value + m.Groups[2].Value);
        return input;
    }

    private void LoadGitInfo()
    {
        var gitDirectory = Repository.Discover(Environment.CurrentDirectory);

        if (string.IsNullOrEmpty(gitDirectory))
        {
            _logManager.Warn("No git directory found!");
            return;
        }

        Repo = new Repository(gitDirectory);

        var branchName = Repo.Head.FriendlyName;
        // Get the short commit hash (7 characters)
        var shortCommitHash = Repo.Head.Tip.Sha.Substring(0, 7);

        BranchName = branchName;
        CommitShortHash = Repo.RetrieveStatus(new StatusOptions()).IsDirty && !_allowDirtyRepo ? "0000000" : shortCommitHash;

        TagNames = Repo.Tags.Where(tag => tag.Target.Sha == Repo.Head.Tip.Sha).Select(tag => tag.FriendlyName).ToArray();
    }

    private bool MatchRefsToConfig(string[] refs, BranchConfig config)
    {
        return refs.Any(t => MatchBranchConfig(t, config));
    }

    private bool MatchBranchConfig(string currentBranch, BranchConfig config)
    {
        foreach (var matchSchema in config.Match)
        {
            var match = Regex.Match(currentBranch, matchSchema);
            if (match.Success)
            {
                _logManager.Trace($"Match found: Regex '{matchSchema}' for branch '{currentBranch}'");
                return true;
            }
        }

        return false;
    }

    #endregion
}