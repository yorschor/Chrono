using Chrono.Core.Helpers;

namespace Chrono.Core.Test;

public class VersionFileTest
{
    [Theory]
    [InlineData("C:/Users/Example/Documents", "C:/Users/Example/Projects/Project1/report.txt", 3)]
    [InlineData("C:/Users/Example/Documents", "C:/Users/Example/Documents/Reports/report.txt", 1)]
    [InlineData("C:/Users/Example/Documents/Reports", "C:/Users/Example/Documents/Reports/report.txt", 0)]
    [InlineData("C:/Users/Example", "C:/Users/Example/report.txt", 0)]
    [InlineData("C:/Users/Example/Documents", "D:/Users/Example/Documents/report.txt", 8)]
    public void TestGetPathDistance(string fromPath, string toPath, int expectedDistance)
    {
        var result = VersionFile.GetPathDistance(fromPath, toPath);
        Assert.Equal(expectedDistance, result);
    }
}