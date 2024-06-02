using System.Text.RegularExpressions;
using Chrono.Core.Helpers;
using LibGit2Sharp;
using NLog;
using Version = System.Version;

namespace Chrono.Core;

public class VersionInfo
{
    #region Properties

    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public int Build { get; private set; }
    public int CiBuildNumber { get; set; }
    public string BranchName { get; private set; }
    public string[] TagNames { get; private set; }
    public string[] CombinedSearchArray => TagNames.Append(BranchName).ToArray();
    public string PrereleaseTag { get; private set; }
    public string CommitShortHash { get; private set; }
    public VersionFile File { get; }

    #endregion

    #region Members

    private readonly Logger _logManager = LogManager.GetCurrentClassLogger();

    private readonly string _versionPath;

    #endregion

    public VersionInfo(string path)
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

        LoadGitInfo();
    }

    /// <summary>
    /// A catch all Method that attempts to resolve and parse a VersionInfo based on the defaults
    /// </summary>
    /// <returns></returns>
    public static Result<VersionInfo> Get()
    {
        var repoFoundResult = GitUtil.GetRepoRootPath();
        if (repoFoundResult is IErrorResult repoErr)
        {
            return Result.Error<VersionInfo>(repoErr);
        }

        var versionFileFoundResult = VersionFile.Find(
            Directory.GetCurrentDirectory(),
            repoFoundResult.Data);

        if (versionFileFoundResult is IErrorResult verErr)
        {
            return Result.Error<VersionInfo>(verErr);
        }

        return Result.Ok(new VersionInfo(versionFileFoundResult.Data));
    }

    /// <summary>
    /// Retrieve the current branch config and return the parsed version for the current branch
    /// </summary>
    /// <param name="schema"></param>
    /// <returns></returns>
    public Result<string> ParseVersion(string schema = "")
    {
        try
        {
            if (string.IsNullOrEmpty(schema))
            {
                _logManager.Trace("No schema provided. Trying to resolve schema from branch config");
                var branchConfig = GetConfigForCurrentBranch();
                if (branchConfig is not IErrorResult)
                {
                    schema = branchConfig.Data.VersionSchema;
                    PrereleaseTag = branchConfig.Data.PrereleaseTag;
                }
                else
                {
                    schema = File.Default.VersionSchema;
                    PrereleaseTag = File.Default.PrereleaseTag;
                    _logManager.Trace("No branch could be matched. Using default configuration!");
                }

                if (string.IsNullOrEmpty(schema))
                {
                    return Result.Error<string>("No schema could be configured");
                }

                _logManager.Trace($"Schema: {schema}");
                _logManager.Trace($"PrereleaseTag: {PrereleaseTag}");
            }

            var schemaWithValues = schema
                .Replace("{major}", Major.ToString())
                .Replace("{minor}", Minor.ToString())
                .Replace("{patch}", Patch.ToString())
                .Replace("{branch}", BranchName)
                .Replace("{prereleaseTag}", PrereleaseTag)
                .Replace("{commitShortHash}", CommitShortHash)
                .Replace("{buildNumber}", CiBuildNumber.ToString());
            schemaWithValues = ResolveDelimiterBlock(schemaWithValues);
            return Result.Ok(ValidateVersionString(schemaWithValues));
        }
        catch (Exception e)
        {
            return Result.Error<string>(e.ToString());
        }
    }

    /// <summary>
    /// Gets the numeric part of the parsed version ignoring any suffixes 
    /// </summary>
    /// <returns></returns>
    public Result<string> GetNumericVersion()
    {
        var parseResult = ParseVersion();
        if (parseResult.Success)
        {
            var numericVersionMatch = RegexPatterns.NumericVersionOnlyRegex.Match(parseResult.Data);
            if (numericVersionMatch.Success)
            {
                return Result.Ok(numericVersionMatch.Value);
            }
        }

        return Result.Error<string>("Could not parse numeric version");
    }

    /// <summary>
    /// Sets the provided version as the new one and saves the version.yml
    /// </summary>
    /// <param name="newVersion"></param>
    /// <returns></returns>
    public Result SetVersion(string newVersion)
    {
        var newVersionMatch = RegexPatterns.ValidVersionRegex.Match(newVersion);
        var fileVersionMatch = RegexPatterns.ValidVersionRegex.Match(File.Version);

        if (!newVersionMatch.Success)
        {
            return Result.Error<string>($"{newVersion} is not a valid version!");
        }

        if (!fileVersionMatch.Success)
        {
            _logManager.Trace($"Info: Version present in version.yml was not correct -> '{File.Version}'");
        }

        Major = int.TryParse(newVersionMatch.Groups[1].Value, out var major) ? major : -1;
        Minor = int.TryParse(newVersionMatch.Groups[2].Value, out var minor) ? minor : -1;
        Patch = int.TryParse(newVersionMatch.Groups[3].Value, out var patch) ? patch : -1;
        Build = int.TryParse(newVersionMatch.Groups[4].Value, out var build) ? build : -1;
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

    #region Internal

    private string ValidateVersionString(string version)
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
        var repoPath = Repository.Discover(Environment.CurrentDirectory);

        if (repoPath == null)
        {
            _logManager.Warn("No Git repository found!");
            return;
        }

        using var repo = new Repository(repoPath);

        var branch = repo.Head;
        var branchName = branch.FriendlyName;

        var commit = branch.Tip;
        // Get the short commit hash (7 characters)
        var shortCommitHash = commit.Sha.Substring(0, 7);

        BranchName = branchName;
        CommitShortHash = shortCommitHash;
        TagNames = repo.Tags.Where(tag => tag.Target.Sha == commit.Sha).Select(tag => tag.FriendlyName).ToArray();
    }

    private Result<BranchConfig> GetConfigForCurrentBranch()
    {
        if (MatchRefsToConfig(CombinedSearchArray, File.Default.Release))
        {
            return Result.Ok(File.Default.Release);
        }

        foreach (var branch in File.Branches)
        {
            if (MatchRefsToConfig(CombinedSearchArray, branch.Value))
            {
                return Result.Ok(branch.Value);
            }
        }

        return Result.Error<BranchConfig>("Something went wrong while trying to get current branch");
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