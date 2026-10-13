using Chrono.Core.GitInfo;

namespace Chrono.Core.Test;

[Collection("EnvVars")]
public class GenericEnvProviderTest : IDisposable
{
    private static readonly string[] Vars =
        [GenericEnvProvider.CiVar, GenericEnvProvider.BranchVar, GenericEnvProvider.TagVar, GenericEnvProvider.CommitShaVar];

    private readonly string _tempDir = Directory.CreateTempSubdirectory("chrono-generic").FullName;

    public GenericEnvProviderTest() => Clear();

    public void Dispose()
    {
        Clear();
        Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    private static void Clear()
    {
        foreach (var v in Vars) Environment.SetEnvironmentVariable(v, null);
    }

    [Fact]
    public void Get_WithChronoCi_ReturnsGenericProvider()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.CiVar, "true");
        Assert.IsType<GenericEnvProvider>(GitInfo.GitInfo.Get());
    }

    [Fact]
    public void Get_WithoutChronoCi_IgnoresOtherChronoVars()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "trunk");
        Environment.SetEnvironmentVariable(GenericEnvProvider.TagVar, "v1.0.0");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "0123456789abcdef");
        Assert.IsNotType<GenericEnvProvider>(GitInfo.GitInfo.Get());
    }

    [Fact]
    public void LoadGitInfo_WithoutCommitSha_Fails()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "trunk");

        var result = new GenericEnvProvider().LoadGitInfo(false);

        Assert.False(result.Success);
        Assert.Contains(GenericEnvProvider.CommitShaVar, result.Message);
    }

    [Fact]
    public void LoadGitInfo_WithoutBranchAndTag_Fails()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "0123456789abcdef");

        var result = new GenericEnvProvider().LoadGitInfo(false);

        Assert.False(result.Success);
        Assert.Contains(GenericEnvProvider.BranchVar, result.Message);
    }

    [Fact]
    public void LoadGitInfo_WithCommitAndTagOnly_Succeeds()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.TagVar, "v1.0.0");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "0123456789abcdef");

        Assert.True(new GenericEnvProvider().LoadGitInfo(false).Success);
    }

    [Fact]
    public void Get_WithChronoCiButMissingVars_ReturnsFailure()
    {
        File.WriteAllText(Path.Combine(_tempDir, "version.yml"), "version: '1.2.3'\ndefault:\n  versionSchema: '{major}.{minor}.{patch}'\n");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CiVar, "true");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _tempDir);

        Assert.False(result.Success);
        Assert.Contains(GenericEnvProvider.CommitShaVar, result.Message);
    }

    [Fact]
    public void Provider_StripsRefPrefixAndShortensHash()
    {
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "refs/heads/release/1.0");
        Environment.SetEnvironmentVariable(GenericEnvProvider.TagVar, "refs/tags/v1.0.0");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "0123456789abcdef");

        var p = new GenericEnvProvider();

        Assert.Equal("release/1.0", p.BranchName);
        Assert.Equal("v1.0.0", p.TagName);
        Assert.Equal("0123456", p.CommitShortHash);
    }

    [Fact]
    public void Get_WithoutGitRepo_UsesEnvAndFindsVersionFileUpwards()
    {
        File.WriteAllText(Path.Combine(_tempDir, "version.yml"), """
            version: '1.2.3'
            default:
              versionSchema: '{major}.{minor}.{patch}[-]{branch}[-]{commitShortHash}'
              release:
                match:
                  - '^release/.*'
                versionSchema: '{major}.{minor}.{patch}'
            """);
        Environment.SetEnvironmentVariable(GenericEnvProvider.CiVar, "true");
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "feature/x");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "abcdef0123");
        var sub = Directory.CreateDirectory(Path.Combine(_tempDir, "sub")).FullName;

        var result = VersionInfo.Get(useEnvVars: true, rootPath: sub);

        Assert.True(result.Success, result.Message);
        Assert.Equal("1.2.3-feature-x-abcdef0", result.Data.GetVersion().Data);
    }

    [Fact]
    public void Get_WithoutGitRepo_ReleaseBranchMatches()
    {
        File.WriteAllText(Path.Combine(_tempDir, "version.yml"), """
            version: '1.2.3'
            default:
              versionSchema: '{major}.{minor}.{patch}-dev'
              release:
                match:
                  - '^release/.*'
                versionSchema: '{major}.{minor}.{patch}'
            """);
        Environment.SetEnvironmentVariable(GenericEnvProvider.CiVar, "true");
        Environment.SetEnvironmentVariable(GenericEnvProvider.BranchVar, "release/1.2");
        Environment.SetEnvironmentVariable(GenericEnvProvider.CommitShaVar, "abcdef0123");

        var result = VersionInfo.Get(useEnvVars: true, rootPath: _tempDir);

        Assert.True(result.Success, result.Message);
        Assert.Equal("1.2.3", result.Data.GetVersion().Data);
    }
}
