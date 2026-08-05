using LibGit2Sharp;

namespace Chrono.Core.Test;

public class VersionInfoWorktreeTest : IDisposable
{
    private const string VersionYamlContent = """
                                                version: '1.2.3'
                                                default:
                                                  versionSchema: '{major}.{minor}.{patch}'
                                                  newBranchSchema: '{branch}schema'
                                                  newTagSchema: 'tagSchema'
                                                  precision: 'minor'
                                                  prereleaseTag: 'beta'
                                                  release:
                                                    match:
                                                      - 'release'
                                                    versionSchema: '{major}.{minor}.{patch}'
                                                    newBranchSchema: 'releaseSchema[-]{branch}'
                                                    newTagSchema: 'tagSchema'
                                                    precision: 'major'
                                                    prereleaseTag: 'beta'
                                                """;

    private readonly string _mainRepoDirectory;
    private readonly string _worktreeDirectory;

    public VersionInfoWorktreeTest()
    {
        _mainRepoDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_mainRepoDirectory);
        File.WriteAllText(Path.Combine(_mainRepoDirectory, "version.yml"), VersionYamlContent);

        Repository.Init(_mainRepoDirectory);
        using (var repo = new Repository(_mainRepoDirectory))
        {
            Commands.Stage(repo, "version.yml");
            repo.Commit("Initial commit", new Signature("Tester", "tester@example.com", DateTime.Now),
                new Signature("Tester", "tester@example.com", DateTime.Now));
        }

        _worktreeDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using (var repo = new Repository(_mainRepoDirectory))
        {
            repo.Worktrees.Add(repo.Head.Tip.Sha, "chrono-test-worktree", _worktreeDirectory, false);
        }

        // LibGit2Sharp's WorktreeCollection.Add sets up the worktree's admin dir and HEAD but does not
        // reliably materialize the tracked files on disk - force an explicit checkout against the new
        // worktree so version.yml actually lands in _worktreeDirectory.
        using (var worktreeRepo = new Repository(_worktreeDirectory))
        {
            Commands.Checkout(worktreeRepo, worktreeRepo.Head, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
        }
    }

    public void Dispose()
    {
        DeleteDirectory(_worktreeDirectory);
        DeleteDirectory(_mainRepoDirectory);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Get_FromLinkedWorktree_ResolvesVersionFile()
    {
        var result = VersionInfo.Get(rootPath: _worktreeDirectory);

        Assert.True(result.Success, result.Message);
        Assert.Equal("1.2.3", result.Data.File.Version);
    }

    [Fact]
    public void Get_FromMainWorktree_StillResolvesVersionFile()
    {
        var result = VersionInfo.Get(rootPath: _mainRepoDirectory);

        Assert.True(result.Success, result.Message);
        Assert.Equal("1.2.3", result.Data.File.Version);
    }

    private static void DeleteDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        // Git marks some internal files (loose objects, etc.) read-only, ->
        // Directory.Delete then throws UnauthorizedAccessException unless the attribute is cleared first
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(directory, true);
    }
}
