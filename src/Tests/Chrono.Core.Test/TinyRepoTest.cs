using Chrono.Core.Helpers;
using LibGit2Sharp;

namespace Chrono.Core.Test;

public class TinyRepoTest
{
    private readonly CoreTestHelper _coreTestHelper = new();
    private string RepoDir => _coreTestHelper.TempDirectory;

    [Fact]
    public void Discover_ShouldFindRepository()
    {
        Directory.SetCurrentDirectory(RepoDir);
        // Act
        var result = TinyRepo.Discover();

        // Assert
        Assert.True(result);
        Assert.Equal(_coreTestHelper.TempDirectory, result.Data.GitDirectory);
    }

    [Fact]
    public void GetCurrentBranch_ShouldReturnTrunkBranch()
    {
        Directory.SetCurrentDirectory(RepoDir);
        // Arrange
        var repo = new Repository(_coreTestHelper.TempDirectory);

        // Act
        var tinyRepo = TinyRepo.Discover().Data;
        var branchName = tinyRepo.GetCurrentBranchName();

        // Assert
        Assert.Equal(repo.Head.FriendlyName, branchName);
    }

    [Fact]
    public void GetCurrentCommit_ShouldReturnCorrectCommit()
    {
        Directory.SetCurrentDirectory(RepoDir);
        // Arrange
        var repo = new Repository(_coreTestHelper.TempDirectory);
        // Act
        var tinyRepo = TinyRepo.Discover().Data;
        var currentCommit = tinyRepo.GetCurrentCommit();
        var commit = repo.Head.Tip;
        // Assert
        Assert.Equal(commit.Sha, currentCommit.Hash);
        Assert.Equal(commit.Message.Trim(), currentCommit.Message.Trim());
    }

    [Fact]
    public void GetTagsPointingToCurrentCommit_ShouldReturnCorrectTags()
    {
        Directory.SetCurrentDirectory(RepoDir);
        // Arrange
        var repo = new Repository(_coreTestHelper.TempDirectory);
        var author = new Signature("Author", "author@example.com", DateTime.Now);
        // var committer = author;
        var commit = repo.Head.Tip;
        var tag = repo.Tags.Add("v1.0", commit, author, "Tag message");

        // Act
        var tinyRepo = TinyRepo.Discover().Data;
        var tags = tinyRepo.GetTagsPointingToCurrentCommit();

        // Assert
        Assert.Single(tags);
        Assert.Equal(tag.FriendlyName, tags.First().Name);
        Assert.Equal(commit.Sha, tags.First().CommitHash);
    }
    
}