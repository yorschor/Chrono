namespace Chrono.Core.Test;

/// <summary>
/// set and bump edit the version file as text. Only the top-level "version:" value may change.
/// </summary>
public class VersionFileSetVersionTest : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("chrono-set-version").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, true);
        GC.SuppressFinalize(this);
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    // "version:" also appears in a comment, in a trailing comment and inside a schema string.
    private const string FileWithVersionLikeText =
        "# The version: line below is the only one that changes\n" +
        "version: 0.1.0   # current version: edit with chrono\n" +
        "default:\n" +
        "  versionSchema: '{major}.{minor}.{patch}'\n" +
        "  newTagSchema: 'version: v{major}.{minor}.{patch}'\n";

    [Fact]
    public void SetVersion_ChangesOnlyTheTopLevelVersionValue()
    {
        var path = Write("version.yml", FileWithVersionLikeText);
        var versionInfo = new VersionInfo(path, new VersionInfoTest.MockGitInfoProvider(), true);

        var result = versionInfo.SetVersion("2.1.0");

        Assert.True(result.Success, result.Message);
        Assert.Equal(FileWithVersionLikeText.Replace("version: 0.1.0   #", "version: 2.1.0   #"),
            File.ReadAllText(path));
    }

    [Fact]
    public void BumpVersion_ChangesOnlyTheTopLevelVersionValue()
    {
        var path = Write("version.yml", FileWithVersionLikeText);
        var versionInfo = new VersionInfo(path, new VersionInfoTest.MockGitInfoProvider(), true);

        var result = versionInfo.BumpVersion(VersionComponent.Minor);

        Assert.True(result.Success, result.Message);
        Assert.Equal(FileWithVersionLikeText.Replace("version: 0.1.0   #", "version: 0.2.0   #"),
            File.ReadAllText(path));
    }

    [Fact]
    public void SetVersion_KeepsQuotesAndCrLfLineEndings()
    {
        var content = "version: '1.0.0'\r\ndefault:\r\n  versionSchema: '{major}.{minor}.{patch}'\r\n";
        var path = Write("crlf.yml", content);
        var versionInfo = new VersionInfo(path, new VersionInfoTest.MockGitInfoProvider(), true);

        var result = versionInfo.SetVersion("3.0.0");

        Assert.True(result.Success, result.Message);
        Assert.Equal(content.Replace("'1.0.0'", "'3.0.0'"), File.ReadAllText(path));
    }

    [Fact]
    public void From_UnknownKey_StillFails()
    {
        var path = Write("version.yml", "version: 1.0.0\nunknownKey: x\ndefault:\n  versionSchema: '{major}'\n");

        var result = VersionFile.From(path);

        Assert.False(result.Success);
    }
}
