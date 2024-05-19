using System.Text.RegularExpressions;
using LibGit2Sharp;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Version = System.Version;

namespace Chrono.Core;

public partial class VersionInfo
{
    #region Properties

    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public int Build { get; set; }
    public string BranchName { get; set; }
    public string PrereleaseTag { get; set; }
    public string CommitShortHash { get; set; }
    public VersionFileModel File { get; }

    #endregion

    #region Members

    private Logger _logManager = LogManager.GetCurrentClassLogger();

    private string _versionPath = "";

    #endregion

    #region RegExPartials

    [GeneratedRegex(@"(\[[^\]]*\])(?=\[[^\]]*\])")]
    private static partial Regex DuplicateBlocksRegex();

    [GeneratedRegex(@"(\[[^\]]*\])$")]
    private static partial Regex EndBlockRegex();

    [GeneratedRegex(@"\{([^\}]*)\}|\[([^\]]*)\]")]
    private static partial Regex BlockContentRegex();

    [GeneratedRegex(@"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$")]
    private static partial Regex ValidVersionRegex();

    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex ValidSemVersionRegex();

    #endregion

    public VersionInfo(string path)
    {
        _versionPath = path;
        File = VersionFileModel.From(_versionPath);
        if (Version.TryParse(File.Version, out var version))
        {
            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build;
            Build = version.Revision;
        }

        LoadGitInfo();
    }

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
                    return new ErrorResult<string>("No schema could be configured");
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
                .Replace("{commitShortHash}", CommitShortHash);
            schemaWithValues = ResolveDelimiterBlock(schemaWithValues);
            return new SuccessResult<string>(ValidateVersionString(schemaWithValues));
        }
        catch (Exception e)
        {
            return new ErrorResult<string>(e.ToString());
        }
    }

    public Result SetVersion(string newVersion)
    {
        var newVersionMatch = ValidVersionRegex().Match(newVersion);
        var fileVersionMatch = ValidVersionRegex().Match(File.Version);

        if (!newVersionMatch.Success)
        {
            return new ErrorResult($"{newVersion} is not a valid version!");
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
            return new ErrorResult("Some went wrong while parsing major and minor values");
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
        return saveResult is IErrorResult ? saveResult : new SuccessResult();
    }

    #region Internal

    private string ValidateVersionString(string version)
    {
        return version.Replace('/', '-');
    }

    private static string ResolveDelimiterBlock(string input)
    {
        input = DuplicateBlocksRegex().Replace(input, "");
        input = EndBlockRegex().Replace(input, "");
        input = BlockContentRegex().Replace(input, m => m.Groups[1].Value + m.Groups[2].Value);
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
        var shortCommitHash = commit.Sha.Substring(0, 7); // Get the short commit hash (7 characters)

        BranchName = branchName;
        CommitShortHash = shortCommitHash;
    }

    private Result<BranchConfig> GetConfigForCurrentBranch()
    {
        if (MatchBranchConfig(BranchName, File.Default.Release))
        {
            return new SuccessResult<BranchConfig>(File.Default.Release);
        }

        foreach (var branch in File.Branches)
        {
            if (MatchBranchConfig(BranchName, branch.Value))
            {
                return new SuccessResult<BranchConfig>(branch.Value);
            }
        }

        return new ErrorResult<BranchConfig>("Something went wrong while trying to get current branch");
    }

    private bool MatchBranchConfig(string currentBranch, BranchConfig config)
    {
        foreach (var matchSchema in config.Match)
        {
            var match = Regex.Match(currentBranch, matchSchema);
            if (match.Success)
            {
                _logManager.Trace($"Matched found: Regex '{matchSchema}' for branch '{currentBranch}'");
                return true;
            }
        }

        return false;
    }

    #endregion
}