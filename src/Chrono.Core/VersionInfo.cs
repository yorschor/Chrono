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

    #endregion

    #region RegExPartials

    [GeneratedRegex(@"(\[[^\]]*\])(?=\[[^\]]*\])")]
    private static partial Regex DuplicateBlocksRegex();

    [GeneratedRegex(@"(\[[^\]]*\])$")]
    private static partial Regex EndBlockRegex();

    [GeneratedRegex(@"\{([^\}]*)\}|\[([^\]]*)\]")]
    private static partial Regex BlockContentRegex();

    #endregion

    public VersionInfo(string path)
    {
        File = VersionFileModel.From(path);
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
                .Replace("{branchName}", BranchName)
                .Replace("{prereleaseTag}", PrereleaseTag)
                .Replace("{commitShortHash}", CommitShortHash);
            return new SuccessResult<string>(ResolveDelimiterBlock(schemaWithValues));
        }
        catch (Exception e)
        {
            return new ErrorResult<string>(e.ToString());
        }
    }

    #region Internal

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
            _logManager.Warn("No Git repository found.");
            return;
        }

        using var repo = new Repository(repoPath);

        var branch = repo.Head;
        var branchName = branch.FriendlyName;

        var commit = branch.Tip;
        var shortCommitHash = commit.Sha.Substring(0, 7); // Get the short commit hash (7 characters)

        BranchName = branchName.Replace('/', '-');
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
                _logManager.Trace($"Matched BranchConfig with regex {matchSchema} for branch {currentBranch}");
                return true;
            }
        }

        return false;
    }

    #endregion
}