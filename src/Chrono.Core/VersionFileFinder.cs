using System.Diagnostics;
using Chrono.Core.Helpers;
using NLog;
using Nuke.Common.IO;
using Nuke.Common.Utilities;

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
            if (!IsSubdirectory(startDirectory, Path.GetDirectoryName(enumerable[0])))
            {
                return new SuccessResult<string>(enumerable[0]);
            }
            return new ErrorResult<string>("The file is in a subdirectory of the start directory.");
        }


        string nearestFile = null;
        var minDistance = int.MaxValue;

        foreach (var file in enumerable)
        {
            if (IsSubdirectory(startDirectory, Path.GetDirectoryName(file)))
            {
                continue;
            }
            var distance = GetPathDistance(startDirectory, file);
            Logger.Trace(distance);
            if (distance < 0 || distance >= minDistance) continue;
            minDistance = distance;
            nearestFile = file;
        }

        return nearestFile != null
            ? new SuccessResult<string>(nearestFile)
            : new ErrorResult<string>("Something went wrong while searching for version.yml");
    }
    
    
    internal static bool IsSubdirectory(string baseDir, string potentialSubDir)
    {
        var baseDirInfo = new DirectoryInfo(baseDir);
        var potentialSubDirInfo = new DirectoryInfo(potentialSubDir);

        while (potentialSubDirInfo.Parent != null)
        {
            if (potentialSubDirInfo.Parent.FullName == baseDirInfo.FullName)
            {
                return true;
            }
            potentialSubDirInfo = potentialSubDirInfo.Parent;
        }

        return false;
    }

    internal static int GetPathDistance(string fromPath, string toPath)
    {
        var absolut = AbsolutePath.Create(fromPath);
        var relative= absolut.GetRelativePathTo(toPath);
        return relative.ToString().Split(Path.DirectorySeparatorChar).Length -1;
    }
}