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
}