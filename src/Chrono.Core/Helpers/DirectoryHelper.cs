using Huxy;
using NLog;
using Nuke.Common.IO;

namespace Chrono.Core.Helpers;

public static class DirectoryHelper
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string[] VersionFileExtensions = [".yml", ".yaml"];

    /// <summary>
    /// Finds the specified target file within the directory hierarchy. Matching is case-insensitive; a name ending
    /// in ".yml" or ".yaml" (like the default "version.yml") also matches the same name with the other extension.
    /// </summary>
    /// <param name="startDirectory">The starting directory for the search.</param>
    /// <param name="stopDirectory">The stopping directory for the search.</param>
    /// <param name="targetFileName">The name of the target file. Defaults to "version.yml".</param>
    /// <returns>A <see cref="Result"/> containing the path to the file or an error message.</returns>
    public static Result<string> Find(string startDirectory, string stopDirectory, string targetFileName = "version.yml")
    {
        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            return Result.Fail<string>("Directory paths and file name cannot be null or empty!");
        }

        return Find(startDirectory, stopDirectory, [targetFileName]);
    }

    /// <summary>
    /// Finds a version file whose name is one of <paramref name="targetFileNames"/> (matched case-insensitively)
    /// within the directory hierarchy. Each name is handled like in <see cref="Find(string,string,string)"/>.
    /// The file nearest to <paramref name="startDirectory"/> wins. If two candidates are equally near, the one whose
    /// name comes first in the list wins.
    /// </summary>
    /// <param name="startDirectory">The starting directory for the search.</param>
    /// <param name="stopDirectory">The stopping directory for the search.</param>
    /// <param name="targetFileNames">File names to look for, in order of preference.</param>
    /// <returns>A <see cref="Result"/> containing the path to the file or an error message.</returns>
    public static Result<string> Find(string startDirectory, string stopDirectory, IReadOnlyList<string> targetFileNames)
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || string.IsNullOrWhiteSpace(stopDirectory) ||
            targetFileNames is null || targetFileNames.Count == 0 || targetFileNames.Any(string.IsNullOrWhiteSpace))
        {
            return Result.Fail<string>("Directory paths and file name cannot be null or empty!");
        }
        startDirectory = Path.GetFullPath(startDirectory);
        var candidateNames = ExpandCandidateFileNames(targetFileNames);
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
        var minNameIndex = int.MaxValue;

        foreach (var file in enumerable)
        {
            if (IsSubdirectory(startDirectory, Path.GetDirectoryName(file)))
            {
                continue;
            }

            var distance = GetPathDistance(startDirectory, file);
            Logger.Trace(distance);
            if (distance < 0) continue;

            // Equally near files: the name that comes first in the list wins.
            var nameIndex = IndexOfName(candidateNames, Path.GetFileName(file));
            if (distance > minDistance || (distance == minDistance && nameIndex >= minNameIndex)) continue;
            minDistance = distance;
            minNameIndex = nameIndex;
            nearestFile = file;
        }

        return nearestFile != null
            ? Result.Ok(nearestFile)
            : Result.Fail<string>($"Something went wrong while searching for {string.Join(" or ", candidateNames)}");
    }

    /// <summary>
    /// Splits a user supplied list of version file names (separated by ',' or ';') into single names.
    /// An empty value gives the default "version.yml".
    /// </summary>
    public static IReadOnlyList<string> ParseFileNames(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ["version.yml"];
        }

        return value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .ToArray();
    }

    /// <summary>
    /// Checks that every entry is a plain file name. A path is rejected because the lookup walks directories itself.
    /// </summary>
    public static Result ValidateFileNames(IReadOnlyList<string> fileNames)
    {
        if (fileNames is null || fileNames.Count == 0)
        {
            return Result.Fail("At least one version file name is required.");
        }

        foreach (var name in fileNames)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(['/', '\\']) >= 0 || name is "." or "..")
            {
                return Result.Fail($"'{name}' is not a valid version file name. Use a plain file name like 'component.yml', not a path.");
            }
        }

        return Result.Ok();
    }

    private static int IndexOfName(string[] candidateNames, string fileName)
    {
        for (var i = 0; i < candidateNames.Length; i++)
        {
            if (string.Equals(candidateNames[i], fileName, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return int.MaxValue;
    }

    private static string[] ExpandCandidateFileNames(IEnumerable<string> targetFileNames) =>
        targetFileNames.SelectMany(GetCandidateFileNames).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();


    /// <summary>
    /// Finds the version file by walking from <paramref name="startDirectory"/> up to the filesystem root.
    /// Used when no git repository is available. Same file name matching as <see cref="Find"/>.
    /// </summary>
    public static Result<string> FindUpwards(string startDirectory, string targetFileName = "version.yml")
    {
        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            return Result.Fail<string>("Directory path and file name cannot be null or empty!");
        }

        return FindUpwards(startDirectory, [targetFileName]);
    }

    /// <summary>
    /// Like <see cref="FindUpwards(string,string)"/> for several names. In each directory the name that comes first
    /// in the list wins; a nearer directory always wins over a farther one.
    /// </summary>
    public static Result<string> FindUpwards(string startDirectory, IReadOnlyList<string> targetFileNames)
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || targetFileNames is null || targetFileNames.Count == 0 ||
            targetFileNames.Any(string.IsNullOrWhiteSpace))
        {
            return Result.Fail<string>("Directory path and file name cannot be null or empty!");
        }

        var candidateNames = ExpandCandidateFileNames(targetFileNames);
        for (var dir = new DirectoryInfo(Path.GetFullPath(startDirectory)); dir != null; dir = dir.Parent)
        {
            var match = dir.EnumerateFiles()
                .Where(f => IndexOfName(candidateNames, f.Name) != int.MaxValue)
                .OrderBy(f => IndexOfName(candidateNames, f.Name))
                .FirstOrDefault();
            if (match != null) return Result.Ok(match.FullName);
        }

        return Result.Fail<string>($"No {string.Join(" or ", candidateNames)} present in or above {startDirectory}");
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
        // Any name ending in .yml or .yaml also matches the other extension, the name as written comes first.
        var extension = Path.GetExtension(targetFileName);
        if (!VersionFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return [targetFileName];
        }

        var baseName = Path.GetFileNameWithoutExtension(targetFileName);
        return VersionFileExtensions
            .OrderBy(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(ext => baseName + ext)
            .ToArray();
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