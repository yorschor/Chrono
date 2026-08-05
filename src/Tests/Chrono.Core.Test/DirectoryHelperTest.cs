using Chrono.Core.Helpers;

namespace Chrono.Core.Test;

public class DirectoryHelperTest
{
    [Fact]
    public void Find_ValidDirectories_ReturnsFilePath()
    {
        // Arrange
        var startDirectory = Directory.GetCurrentDirectory();
        var stopDirectory = Directory.GetCurrentDirectory();
        var targetFileName = "sample_version.yml";

        // Act
        var result = DirectoryHelper.Find(startDirectory, stopDirectory, targetFileName);

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
        var result = DirectoryHelper.IsSubdirectory(baseDir, subDir);

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
        var distance = DirectoryHelper.GetPathDistance(fromPath, toPath);

        // Assert
        Assert.Equal(1, distance);
    }

    [Theory]
    [InlineData("version.yml")]
    [InlineData("version.yaml")]
    [InlineData("VERSION.YML")]
    [InlineData("Version.Yaml")]
    public void Find_DefaultTargetFileName_MatchesYmlAndYamlCaseInsensitively(string actualFileName)
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        var expectedPath = Path.Combine(tempDirectory, actualFileName);
        File.WriteAllText(expectedPath, "version: '1.0.0'");

        try
        {
            // Act
            var result = DirectoryHelper.Find(tempDirectory, tempDirectory);

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.Equal(expectedPath, result.Data);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void Find_ExplicitTargetFileName_DoesNotAliasToOtherExtensions()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "custom.yaml"), "version: '1.0.0'");

        try
        {
            // Act
            var result = DirectoryHelper.Find(tempDirectory, tempDirectory, "custom.yml");

            // Assert
            Assert.False(result.Success);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}