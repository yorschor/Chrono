using Huxy;
using LibGit2Sharp;
using NLog;

namespace Chrono.Core.GitInfo;

public class GitRepoProvider : IGitInfoProvider
{
    public Repository Repo { get; private set; }
    public string[] TagNames { get;  set; }
    public string BranchName { get;  set; }
    public string TagName { get;  set; }
    
    public string CommitHash { get; set; }
    public string CommitShortHash { get; set; }

    private bool _isInDetachedHead;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "")
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
        var commitHash = Repo.Head.Tip.Sha;
        var shortCommitHash = commitHash.Substring(0, 7);

        BranchName = branchName;
        CommitHash = Repo.RetrieveStatus(new StatusOptions()).IsDirty && !allowDirtyRepo
            ? dirtyRepoPlaceholder
            : commitHash;
        CommitShortHash = Repo.RetrieveStatus(new StatusOptions()).IsDirty && !allowDirtyRepo
            ? dirtyRepoPlaceholder
            : shortCommitHash;

        TagNames = Repo.Tags.Where(tag => tag.Target.Sha == Repo.Head.Tip.Sha).Select(tag => tag.FriendlyName)
            .ToArray();
        _isInDetachedHead = Repo.Info.IsHeadDetached;
        if (_isInDetachedHead)
        {
            TagName = ParseReflogEntry(Repo.Refs.Log(Repo.Head.Reference).First());
        }

        return Result.Ok();
    }

    private string ParseReflogEntry(ReflogEntry entry)
    {
        var message = entry.Message;

        if (message.StartsWith("checkout: moving from"))
        {
            var parts = message.Split([" to "], StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var destination = parts[1].Trim();

                // Check if the destination is a tag
                if (Repo.Tags.Any(tag => tag.FriendlyName == destination))
                {
                    _logger.Trace($"Found detached HEAD due to checkout of tag: {destination}");
                    return destination;
                }

                _logger.Trace(Repo.Branches.Any(branch => branch.FriendlyName == destination)
                    ? $"Found detached HEAD due to move to branch: {destination}" // 'HEAD' moved to a branch (unlikely in detached state)
                    : $"Found detached HEAD at commit: {destination}");
            }
        }

        return string.Empty;
    }
}