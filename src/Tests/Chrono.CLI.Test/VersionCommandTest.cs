using System.Diagnostics;
using LibGit2Sharp;

// ReSharper disable InconsistentNaming

namespace Chrono.CLI.Test;

public class VersionCommandTest
{
    private readonly CliTestHelper App = new();

    [Fact]
    public void GetVersionCommandTest()
    {
        Debug.WriteLine("GetVersionCommandTest...");
        using var repo = new Repository(App.TempDirectory);
        var hash = repo.Head.Tip.Sha;
        App.RunAndAssert(["get"], "1.0.0-trunk." + hash[..7]);
    }
    
    [Fact]
    public void GetVersionOnConfiguredBranchTest()
    {
        Debug.WriteLine("GetVersionOnConfiguredBranchTest...");
        using var repo = new Repository(App.TempDirectory);
        repo.CreateBranch("feature/test");
        LibGit2Sharp.Commands.Checkout(repo, "feature/test");
        var hash = repo.Head.Tip.Sha;
        App.RunAndAssert(["get"], "1.0.0-feature-test." + hash[..7]);
    }

    [Fact]
    public void GetVersionOnReleaseBranchTest()
    {
        Debug.WriteLine("GetVersionOnReleaseBranchTest...");
        using var repo = new Repository(App.TempDirectory);
        repo.CreateBranch("release/v1.0.0");
        LibGit2Sharp.Commands.Checkout(repo, "release/v1.0.0");
        var hash = repo.Head.Tip.Sha;
        App.RunAndAssert(["get"], "1.0.0-rc-" + hash[..7]);
    }
    
    [Fact]
    public void GetVersionOnReleaseTagTest()
    {
        Debug.WriteLine("GetVersionOnReleaseTagTest...");
        using var repo = new Repository(App.TempDirectory);
        repo.ApplyTag("v1.0.0");
        LibGit2Sharp.Commands.Checkout(repo, "v1.0.0");
        App.RunAndAssert(["get"], "1.0.0");
    }
    [Fact]
    public void SetVersionCommandTest()
    {
        Debug.WriteLine("SetVersionCommandTest...");
        using var repo = new Repository(App.TempDirectory);
        var hash = repo.Head.Tip.Sha;
        
        App.RunAndAssert(["set", "5.6.4"], "");
        App.RunAndAssert(["get"], "5.6.4-trunk." + hash[..7]);
    }
    
    [Fact]
    public void BumpVersionCommandTest()
    {
        Debug.WriteLine("BumpVersionCommandTest...");
        using var repo = new Repository(App.TempDirectory);
        var hash = repo.Head.Tip.Sha;
        App.RunAndAssert(["get"], "1.0.0-trunk." + hash[..7]);
        App.RunAndAssert(["bump", "patch"], "");
        App.RunAndAssert(["get"], "1.0.1-trunk." + hash[..7]);
        App.RunAndAssert(["bump", "minor"], "");
        App.RunAndAssert(["get"], "1.1.0-trunk." + hash[..7]);
        App.RunAndAssert(["bump", "major"], "");
        App.RunAndAssert(["get"], "2.0.0-trunk." + hash[..7]);
    }
}