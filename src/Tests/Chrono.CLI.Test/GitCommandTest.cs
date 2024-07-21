// ReSharper disable InconsistentNaming

using System.Diagnostics;
using LibGit2Sharp;

namespace Chrono.CLI.Test;

public class GitCommandTest
{
    private readonly CliTestHelper App = new();

    [Fact]
    public void ReleaseCommandTest()
    {
        Debug.WriteLine("ReleaseCommandTest...");
        using var repo = new Repository(App.TempDirectory);
        var hash = repo.Head.Tip.Sha;
        App.RunAndAssert(["release"], "");
        App.RunAndAssert(["get"], $"1.1.0.trunk." + hash[..7]);
        // check if version is correct on "from" branch
        // Check if release branch is created with correct name
        // check if version is correct in new branch
    }

    [Fact]
    public void TryReleaseFromIncorrectBranchTest()
    {
        Debug.WriteLine("TryReleaseFromIncorrectBranchTest...");
        // Assert that no release branch is created
        // Assert that no version is changed
    }

    [Fact]
    public void TagCommandTest()
    {
        Debug.WriteLine("TagCommandTest...");
        // App.RunAndAssert(["tag"], "1.0.0");
        // Ensure nothing is unstaged
        // Check if tag branch is created with correct name
        // check if version is correct in new branch
    }

    [Fact]
    public void TryTagWithUnstagedChangesTest()
    {
        Debug.WriteLine("TagCommandTest...");
        // App.RunAndAssert(["tag"], "1.0.0");
        // Ensure something is unstaged
        // Check if tag branch is created with correct name
        // check if version is correct in new branch
    }
}