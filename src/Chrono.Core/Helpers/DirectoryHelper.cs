using Huxy;
using NLog;
using Nuke.Common.IO;

namespace Chrono.Core.Helpers;

public static class DirectoryHelper
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string[] VersionFileExtensions = [".yml", ".yaml"];

    /// <summary>
    /// Finds the specified target file within the directory hierarchy. Matching is case-insensitive; if
    /// <paramref name="targetFileName"/> is the default "version.yml", both ".yml" and ".yaml" are accepted.
    /// </summary>
    /// <param name="startDirectory">The starting directory for the search.</param>
    /// <param name="stopDirectory">The stopping directory for the search.</param>
    /// <param name="targetFileName">The name of the target file. Defaults to "version.yml".</param>
    /// <returns>A <see cref="Result"/> containing the path to the file or an error message.</returns>
    public static Result<string> Find(string startDirectory, string stopDirectory, string targetFileName = "version.yml")
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || string.IsNullOrWhiteSpace(targetFileName) || string.IsNullOrWhiteSpace(stopDirectory))
        {
            return Result.Fail<string>("Directory paths and file name cannot be null or empty!");
        }
        startDirectory = Path.GetFullPath(startDirectory);
        var candidateNames = GetCandidateFileNames(targetFileName);
        var files = Directory.EnumerateFiles(stopDirectory, "*", SearchOption.AllDirectories)
            .Where(f => candidateNames.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));
        var enumerable = files as string[] ?? files.ToArray();

        if (!enumerable.Any())
        {
            return Result.Fail<string>($"No {string.Join(" or ", candidateNames)} present");
        }

        Logger.Trace($"Found {enumerable.Length} version file(s)");
        for (var i = 0; i < enumerable.Length; i++)
        {
            Logger.Trace($" \u2514 {i + 1} : {enumerable[i]}");
        }

        if (enumerable.Length == 1)
        {
            if (!IsSubdirectory(startDirectory, Path.GetDirectoryName(enumerable[0])))
            {
                return Result.Ok(enumerable[0]);
            }

            return Result.Fail<string>("The file is in a subdirectory of the start directory.");
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
            ? Result.Ok(nearestFile)
            : Result.Fail<string>("Something went wrong while searching for version.yml");
    }


    /// <summary>
    /// Determines if a directory is a subdirectory of another directory.
    /// </summary>
    /// <param name="baseDir">The base directory.</param>
    /// <param name="potentialSubDir">The potential subdirectory.</param>
    /// <returns>True if the potential subdirectory is a subdirectory of the base directory, otherwise false.</returns>
    public static bool IsSubdirectory(string baseDir, string potentialSubDir)
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

    /// <summary>
    /// Gets the distance between two paths.
    /// </summary>
    /// <param name="fromPath">The starting path.</param>
    /// <param name="toPath">The target path.</param>
    /// <returns>The distance between the two paths.</returns>
    public static int GetPathDistance(string fromPath, string toPath)
    {
        var absolut = AbsolutePath.Create(fromPath);
        var relative = absolut.GetRelativePathTo(toPath);
        return relative.ToString().Split(Path.DirectorySeparatorChar).Length - 1;
    }

    private static string[] GetCandidateFileNames(string targetFileName)
    {
        if (!targetFileName.Equals("version.yml", StringComparison.OrdinalIgnoreCase))
        {
            return [targetFileName];
        }

        var baseName = Path.GetFileNameWithoutExtension(targetFileName);
        return VersionFileExtensions.Select(ext => baseName + ext).ToArray();
    }

    public static string AppendPathsWithPotentialFileName(string path1, string path2, string fileName)
    {
        if (path1.EndsWith(fileName))
        {
            path1 = path1.Substring(0, path1.Length - fileName.Length);
        }
        if (path2.EndsWith(fileName))
        {
            path2 = path2.Substring(0, path2.Length - fileName.Length);
        }
        return Path.Combine(path1, path2, fileName);
    }
}