using LibGit2Sharp;

namespace Chrono.CLI.Test;

public class VersionFileNameCommandTest
{
    private readonly CliTestHelper _app = new();

    /// <summary>Adds and commits component.yml (a copy of version.yml with another version) next to version.yml.</summary>
    private string AddComponentFile(string version)
    {
        var versionText = File.ReadAllText(Path.Combine(_app.TempDirectory, "version.yml"));
        var componentPath = Path.Combine(_app.TempDirectory, "component.yml");
        File.WriteAllText(componentPath, versionText.Replace("version: 1.0.0", $"version: {version}"));

        using var repo = new Repository(_app.TempDirectory);
        LibGit2Sharp.Commands.Stage(repo, "component.yml");
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        repo.Commit("Add component.yml", signature, signature);
        return componentPath;
    }

    private string HeadShortHash()
    {
        using var repo = new Repository(_app.TempDirectory);
        return repo.Head.Tip.Sha[..7];
    }

    [Fact]
    public void Get_WithoutFileOption_StillUsesVersionYml()
    {
        AddComponentFile("9.0.0");

        _app.RunAndAssert(["get"], "1.0.0-trunk." + HeadShortHash());
    }

    [Fact]
    public void Get_WithFileOption_UsesThatFile()
    {
        AddComponentFile("9.0.0");

        _app.RunAndAssert(["get", "--file", "component.yml"], "9.0.0-trunk." + HeadShortHash());
    }

    [Fact]
    public void Get_WithFileList_FirstExistingNameWins()
    {
        AddComponentFile("9.0.0");

        _app.RunAndAssert(["get", "-f", "missing.yml,component.yml,version.yml"], "9.0.0-trunk." + HeadShortHash());
    }

    [Fact]
    public void Bump_WithFileOption_ChangesOnlyThatFile()
    {
        var component = AddComponentFile("9.0.0");
        var versionFile = Path.Combine(_app.TempDirectory, "version.yml");
        var versionBefore = File.ReadAllText(versionFile);

        _app.RunAndAssert(["bump", "patch", "-f", "component.yml"], "");

        Assert.Contains("version: 9.0.1", File.ReadAllText(component));
        Assert.Equal(versionBefore, File.ReadAllText(versionFile));
    }

    [Fact]
    public void Release_WithFileOption_CommitsTheConfiguredFile()
    {
        AddComponentFile("9.0.0");
        var versionFile = Path.Combine(_app.TempDirectory, "version.yml");
        var versionBefore = File.ReadAllText(versionFile);

        _app.RunAndAssert(["release", "-c", "-i", "-f", "component.yml"], "");

        using var repo = new Repository(_app.TempDirectory);
        Assert.NotNull(repo.Branches["release/v9.0"]);
        Assert.Contains("Chrono: Set version", repo.Head.Tip.Message);
        var committed = ((Blob)repo.Head.Tip.Tree["component.yml"].Target).GetContentText();
        Assert.Contains("version: 9.1.0", committed);
        Assert.Equal(versionBefore, File.ReadAllText(versionFile));
    }
}
