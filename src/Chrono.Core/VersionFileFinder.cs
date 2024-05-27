using Chrono.Core.Helpers;
using NLog;
using SearchOption = System.IO.SearchOption;

namespace Chrono.Core;

public class VersionFileFinder
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static Result<string> FindVersionFile(string startDirectory, string stopDirectory, string targetFileName = "version.yml")
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || string.IsNullOrWhiteSpace(targetFileName) || string.IsNullOrWhiteSpace(stopDirectory))
        {
            return new ErrorResult<string>("Directory paths and file name cannot be null or empty.");
        }

        var files = Directory.EnumerateFiles(stopDirectory, targetFileName, SearchOption.AllDirectories);
        var enumerable = files as string[] ?? files.ToArray();
        
        if (!enumerable.Any())
        {
            return new ErrorResult<string>("No version.yml present");
        }
        Logger.Trace($"Found {enumerable.Length} version file(s)");
        for (var i = 0; i < enumerable.Length; i++)
        {
            Logger.Trace($"==> {i + 1} : {enumerable[i]}");
        }

        if (enumerable.Length == 1)
        {
            return new SuccessResult<string>(enumerable[0]);
        }
        
        string nearestFile = null;
        var minDistance = int.MaxValue;

        foreach (var file in enumerable)
        {
            var distance = GetPathDistance(startDirectory, file);
            if (distance < 0 || distance >= minDistance) continue;
            minDistance = distance;
            nearestFile = file;
        }

        return nearestFile != null ? new SuccessResult<string>(nearestFile) : new ErrorResult<string>("Something went wrong while searching for version.yml");
    }

    private static int GetPathDistance(string fromPath, string toPath)
    {
        var fromUri = new Uri(fromPath);
        var toUri = new Uri(toPath);
        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return relativePath.Split(Path.DirectorySeparatorChar).Length - 1;
    }
}