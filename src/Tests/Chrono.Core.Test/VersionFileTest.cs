using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            Precision = "defaultPrecision",
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

    #region YamlMerging

    [Fact]
    public void MergeYamlContent_ValidYaml_ReturnsMergedContent()
    {
        const string baseYaml = """
                                version: "1.0.0"
                                default:
                                  versionSchema: "{major}.{minor}.{patch}"
                                  precision: "minor"  
                                  release:
                                    match:
                                      - "^release/.*"
                                """;

        const string overrideYaml = """
                                    version: "3.2.4"
                                    default:
                                        versionSchema: "{major}.{minor}.{patch}[-]{branchname}[-]{commitShortHash}"
                                    """;

        const string expectedMergedYaml = """
                                          version: "3.2.4"
                                          default:
                                            versionSchema: "{major}.{minor}.{patch}[-]{branchname}[-]{commitShortHash}"
                                            precision: "minor"  
                                            release:
                                              match:
                                                - "^release/.*"
                                          """;

        var mergedYaml = VersionFile.MergeYamlContent(baseYaml, overrideYaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var mergedObject = deserializer.Deserialize(new StringReader(mergedYaml));
        var expectedObject = deserializer.Deserialize(new StringReader(expectedMergedYaml));

        Assert.True(AreEqual(mergedObject, expectedObject), "The merged YAML content does not match the expected content.");
    }


    private bool AreEqual(object? obj1, object? obj2)
    {
        if (obj1 == null || obj2 == null)
        {
            return obj1 == obj2;
        }

        if (obj1.GetType() != obj2.GetType())
        {
            return false;
        }

        switch (obj1)
        {
            case IDictionary<object, object> dict1 when obj2 is IDictionary<object, object> dict2:
            {
                if (dict1.Count != dict2.Count)
                {
                    return false;
                }

                foreach (var key in dict1.Keys)
                {
                    if (!dict2.ContainsKey(key))
                    {
                        return false;
                    }

                    if (!AreEqual(dict1[key], dict2[key]))
                    {
                        return false;
                    }
                }

                return true;
            }
            case IList<object> list1 when obj2 is IList<object> list2:
            {
                if (list1.Count != list2.Count)
                {
                    return false;
                }

                for (int i = 0; i < list1.Count; i++)
                {
                    if (!AreEqual(list1[i], list2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            default:
                return obj1.Equals(obj2);
        }
    }

    #endregion

    ~VersionFileTests()
    {
        // Cleanup sample YAML file
        if (File.Exists(_sampleYamlPath))
        {
            File.Delete(_sampleYamlPath);
        }
    }
}