namespace Chrono.Core.Test;

public class VersionFileTests
{
    private readonly string _sampleYamlPath = "sample_version.yml";

    private readonly string _sampleYamlContent = """

                                                 version: '1.0.0'
                                                 default:
                                                   versionSchema: 'v{major}.{minor}.{patch}'
                                                   newBranchSchema: 'branch-{branch}'
                                                   newTagSchema: 'tag-{tag}'
                                                   precision: 'patch'
                                                   prereleaseTag: 'alpha'
                                                   release:
                                                     match:
                                                       - 'main'
                                                       - 'release'
                                                 branches:
                                                   develop:
                                                     match:
                                                       - 'dev'
                                                     versionSchema: 'v{major}.{minor}.{patch}-dev'
                                                     newBranchSchema: 'branch-dev-{branch}'
                                                     newTagSchema: 'tag-dev-{tag}'
                                                     precision: 'minor'
                                                     prereleaseTag: 'beta'

                                                 """;

    public VersionFileTests()
    {
        // Create a sample YAML file for testing
        File.WriteAllText(_sampleYamlPath, _sampleYamlContent);
    }

    [Fact]
    public void From_ValidPath_ReturnsVersionFile()
    {
        // Arrange & Act
        var versionFile = VersionFile.From(_sampleYamlPath);

        // Assert
        Assert.True(versionFile);
        Assert.Equal("1.0.0", versionFile.Data.Version);
        Assert.NotNull(versionFile.Data.Default);
        Assert.NotNull(versionFile.Data.Branches);
    }

    [Fact]
    public async Task FromAsync_ValidPath_ReturnsVersionFile()
    {
        // Arrange & Act
        var versionFile = await VersionFile.FromAsync(_sampleYamlPath);

        // Assert
        Assert.True(versionFile);
        Assert.Equal("1.0.0", versionFile.Data.Version);
        Assert.NotNull(versionFile.Data.Default);
        Assert.NotNull(versionFile.Data.Branches);
    }

    [Fact]
    public async Task FetchYamlFromUriAsync_ValidUri_ReturnsContent()
    {
        // Arrange
        var uri = "https://raw.githubusercontent.com/yorschor/Chrono/trunk/version.yml";
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        using var httpClient = new HttpClient(httpClientHandler);
        var response = await httpClient.GetStringAsync(uri);

        // Act
        var result = await VersionFile.FetchYamlFromUriAsync(uri);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(response, result.Data);
    }
    
    [Fact]
    public async Task FetchYamlFromUriAsync_ValidFileUri_ReturnsContent()
    {
        // Arrange
        var uri = $"file://{_sampleYamlPath}";
        
        // Act
        var result = await VersionFile.FetchYamlFromUriAsync(uri);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void AccessFallbackBranchProperty_MixedValues_ReturnsCorrectValues()
    {
        // Arrange
        var defaultConfig = new BranchConfig
        {
            Match = ["defaultMatch"],
            VersionSchema = "defaultVersionSchema",
            NewBranchSchema = "defaultNewBranchSchema",
            NewTagSchema = "defaultNewTagSchema",
            Precision = VersionComponent.Minor,
            PrereleaseTag = "defaultPrereleaseTag"
        };

        var specificConfig = new BranchConfig
        {
            // Should fallback to default
            Match = null,
            Precision = null,
            NewBranchSchema = null,

            // Should use specific
            VersionSchema = "specificVersionSchema",
            NewTagSchema = "specificNewTagSchema",
            PrereleaseTag = "specificPrereleaseTag"
        };

        var configWithFallback = new BranchConfigWithFallback(defaultConfig, specificConfig);

        // Act & Assert
        // Properties expected to use defaultConfig values
        Assert.Equal(defaultConfig.Match, configWithFallback.Match);
        Assert.Equal(defaultConfig.NewBranchSchema, configWithFallback.NewBranchSchema);
        Assert.Equal(defaultConfig.Precision, configWithFallback.Precision);

        // Properties expected to use specificConfig values
        Assert.Equal(specificConfig.VersionSchema, configWithFallback.VersionSchema);
        Assert.Equal(specificConfig.NewTagSchema, configWithFallback.NewTagSchema);
        Assert.Equal(specificConfig.PrereleaseTag, configWithFallback.PrereleaseTag);
    }


    ~VersionFileTests()
    {
        // Cleanup sample YAML file
        if (File.Exists(_sampleYamlPath))
        {
            File.Delete(_sampleYamlPath);
        }
    }
}