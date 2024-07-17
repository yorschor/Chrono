namespace Chrono.Core.Test;

public class VersionFileTests
{
    private readonly string _sampleYamlPath = "sample_version.yml";

    private readonly string _sampleYamlContent = @"
version: '1.0.0'
default:
  inheritFrom: 'https://example.com/inherit.yml'
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
";

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
        Assert.NotNull(versionFile);
        Assert.Equal("1.0.0", versionFile.Version);
        Assert.NotNull(versionFile.Default);
        Assert.NotNull(versionFile.Branches);
    }

    [Fact]
    public async Task FromAsync_ValidPath_ReturnsVersionFile()
    {
        // Arrange & Act
        var versionFile = await VersionFile.FromAsync(_sampleYamlPath);

        // Assert
        Assert.NotNull(versionFile);
        Assert.Equal("1.0.0", versionFile.Version);
        Assert.NotNull(versionFile.Default);
        Assert.NotNull(versionFile.Branches);
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
    public void Save_ValidPath_SavesFileSuccessfully()
    {
        // Arrange
        var versionFile = VersionFile.From(_sampleYamlPath);
        var savePath = "saved_version.yml";

        // Act
        var result = versionFile.Save(savePath);

        // Assert
        Assert.True(result.Success);
        Assert.True(File.Exists(savePath));
        File.Delete(savePath); // Cleanup
    }

    [Fact]
    public void Find_ValidDirectories_ReturnsFilePath()
    {
        // Arrange
        var startDirectory = Directory.GetCurrentDirectory();
        var stopDirectory = Directory.GetCurrentDirectory();
        var targetFileName = "sample_version.yml";

        // Act
        var result = VersionFile.Find(startDirectory, stopDirectory, targetFileName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), targetFileName), result.Data);
    }

    [Fact]
    public void IsSubdirectory_ValidPaths_ReturnsTrue()
    {
        // Arrange
        var baseDir = Directory.GetCurrentDirectory();
        var subDir = Path.Combine(Directory.GetCurrentDirectory(), "subdir");

        // Act
        var result = VersionFile.IsSubdirectory(baseDir, subDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetPathDistance_ValidPaths_ReturnsCorrectDistance()
    {
        // Arrange
        var fromPath = Directory.GetCurrentDirectory();
        var toPath = Path.Combine(Directory.GetCurrentDirectory(), "subdir", "file.txt");

        // Act
        var distance = VersionFile.GetPathDistance(fromPath, toPath);

        // Assert
        Assert.Equal(1, distance);
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