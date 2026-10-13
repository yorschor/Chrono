using Chrono.Core.GitInfo;

namespace Chrono.Core.Test;

/// <summary>
/// VersionInfo.Get with a configured version file name. Uses the generic CI provider so no git repository is needed.
/// </summary>
[Collection("EnvVars")]
public class VersionFileNameTest : IDisposable
{
    private static readonly string[] Vars =
        [GenericEnvProvider.CiVar, GenericEnvProvider.BranchVar, GenericEnvProvider.TagVar, GenericEnvProvider.CommitShaVar];

    private readonly string _dir = Directory.CreateTempSubdirectory("chrono-file-name").FullName;

    public VersionFileNameTest()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.CiVar, "true");
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "trunk");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "abcdef0123456789");
    }

    public void Dispose()
    {
        foreach (var v in Vars) Environment.SetEnvironmentVariable(v, null);
        Directory.Delete(_dir, true);
        GC.SuppressFinalize(this);
    }

    private string Write(string name, string version, string? subDirectory = null)
    {
        var directory = subDirectory is null ? _dir : Path.Combine(_dir, subDirectory);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, $"version: {version}\ndefault:\n  versionSchema: '{{major}}.{{minor}}.{{patch}}'\n");
        return path;
    }

    [Fact]
    public void Get_WithFileName_UsesThatFile()
    {
        var path = Write("component.yml", "7.7.7");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _dir, versionFileName: "component.yml");

        Assert.True(result.Success, result.Message);
        Assert.Equal(path, result.Data.VersionFilePath);
        Assert.Equal("7.7.7", result.Data.GetVersion().Data);
    }

    [Fact]
    public void Get_WithoutFileName_DoesNotPickUpOtherNames()
    {
        Write("component.yml", "7.7.7");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _dir);

        Assert.False(result.Success);
        Assert.Contains("version.yml", result.Message);
    }

    [Fact]
    public void Get_WithoutFileName_StillFindsDefaultNamesCaseInsensitively()
    {
        var path = Write("VERSION.YAML", "3.2.1");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _dir);

        Assert.True(result.Success, result.Message);
        Assert.Equal(path, result.Data.VersionFilePath);
    }

    [Fact]
    public void Get_WithTwoNames_PrefersNearestThenListOrder()
    {
        Write("version.yml", "1.0.0");
        var component = Write("component.yml", "2.0.0", "svc");
        var start = Path.Combine(_dir, "svc", "sub");
        Directory.CreateDirectory(start);

        var result = VersionInfo.Get(useEnvVars: true, rootPath: start, versionFileName: "component.yml,version.yml");

        Assert.True(result.Success, result.Message);
        Assert.Equal(component, result.Data.VersionFilePath);
    }

    [Fact]
    public void Get_WithTwoNames_FallsBackToTheSecond()
    {
        var version = Write("version.yml", "1.0.0");
        var start = Path.Combine(_dir, "svc");
        Directory.CreateDirectory(start);

        var result = VersionInfo.Get(useEnvVars: true, rootPath: start, versionFileName: "component.yml;version.yml");

        Assert.True(result.Success, result.Message);
        Assert.Equal(version, result.Data.VersionFilePath);
    }

    [Theory]
    [InlineData("sub/component.yml")]
    [InlineData("..\\component.yml")]
    public void Get_WithPathInsteadOfName_Fails(string name)
    {
        Write("component.yml", "7.7.7");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _dir, versionFileName: name);

        Assert.False(result.Success);
        Assert.Contains("not a valid version file name", result.Message);
    }

    [Fact]
    public void SetVersion_WritesToTheConfiguredFile()
    {
        var component = Write("component.yml", "1.0.0");
        var version = Write("version.yml", "5.0.0");

        var info = VersionInfo.Get(useEnvVars: true, rootPath: _dir, versionFileName: "component.yml").Data;
        var result = info.SetVersion("1.1.0");

        Assert.True(result.Success, result.Message);
        Assert.Contains("version: 1.1.0", File.ReadAllText(component));
        Assert.Contains("version: 5.0.0", File.ReadAllText(version));
    }
}
