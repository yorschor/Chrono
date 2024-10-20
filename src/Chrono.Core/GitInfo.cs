using Huxy;
using LibGit2Sharp;
using NLog;

namespace Chrono.Core;

public class GitInfo
{
    public Repository Repo { get; private set; }
    public string BranchName { get; internal set; }
    public string[] TagNames { get; internal set; }
    public bool IsInDetachedHead { get; internal set; }
    public string CommitShortHash { get; private set; }
    public string LastReflogToTarget { get; private set; }
    
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder ="")
    {
        var gitDirectory = Repository.Discover(Environment.CurrentDirectory);

        if (string.IsNullOrEmpty(gitDirectory))
        {
            return Result.Fail("No git directory found!");
        }

        Repo = new Repository(gitDirectory);

        if (Repo.Info.IsBare || Repo.Info.IsHeadUnborn)
        {
            return Result.Fail("Git Repo appears empty! (Did you forget to initialize it?)");
        }

        var branchName = Repo.Head.FriendlyName;
        // Get the short commit hash (7 characters)
        var shortCommitHash = Repo.Head.Tip.Sha.Substring(0, 7);

        BranchName = branchName;
        // CommitShortHash = shortCommitHash;
        CommitShortHash = Repo.RetrieveStatus(new StatusOptions()).IsDirty && !allowDirtyRepo
            ? dirtyRepoPlaceholder
            : shortCommitHash;

        TagNames = Repo.Tags.Where(tag => tag.Target.Sha == Repo.Head.Tip.Sha).Select(tag => tag.FriendlyName)
            .ToArray();
        IsInDetachedHead = Repo.Info.IsHeadDetached;
        LastReflogToTarget = Repo.Refs.Log(Repo.Head.Reference).First().To.ToString();
        _logger.Trace($"Last reflog to target: {LastReflogToTarget}");
        return Result.Ok();
    }

}