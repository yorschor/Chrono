using Chrono.Core.Helpers;
using NLog;
using Nuke.Common.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Chrono.Core;

/// <summary>
/// Represents a version file with configurations for different branches and default settings.
/// </summary>
public class VersionFile
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [YamlMember(Alias = "version")] public string Version { get; set; }

    [YamlMember(Alias = "default")] public DefaultConfig Default { get; set; }

    [YamlMember(Alias = "branches")] public Dictionary<string, BranchConfig> Branches { get; set; }

    
    /// <summary>
    /// Creates a <see cref="VersionFile"/> instance from the specified YAML file.
    /// </summary>
    /// <param name="path">The path to the YAML file.</param>
    /// <returns>A <see cref="VersionFile"/> instance.</returns>
    public static VersionFile From(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlContent = File.ReadAllText(path);
        return deserializer.Deserialize<VersionFile>(yamlContent);
    }

    /// <summary>
    /// Saves the current instance to the specified path.
    /// </summary>
    /// <param name="path">The path where the file will be saved.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Save(string path)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlContent = serializer.Serialize(this);
            File.WriteAllText(path, yamlContent);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }
    
    /// <summary>
    /// Finds the specified target file within the directory hierarchy.
    /// </summary>
    /// <param name="startDirectory">The starting directory for the search.</param>
    /// <param name="stopDirectory">The stopping directory for the search.</param>
    /// <param name="targetFileName">The name of the target file. Defaults to "version.yml".</param>
    /// <returns>A <see cref="Result{T}"/> containing the path to the file or an error message.</returns>
    public static Result<string> Find(string startDirectory, string stopDirectory, string targetFileName = "version.yml")
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || string.IsNullOrWhiteSpace(targetFileName) || string.IsNullOrWhiteSpace(stopDirectory))
        {
            return Result.Error<string>("Directory paths and file name cannot be null or empty.");
        }

        var files = Directory.EnumerateFiles(stopDirectory, targetFileName, SearchOption.AllDirectories);
        var enumerable = files as string[] ?? files.ToArray();

        if (!enumerable.Any())
        {
            return Result.Error<string>("No version.yml present");
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
                return Result.Ok(enumerable[0]);
            }

            return Result.Error<string>("The file is in a subdirectory of the start directory.");
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
            : Result.Error<string>("Something went wrong while searching for version.yml");
    }

    #region Helpers

    /// <summary>
    /// Determines if a directory is a subdirectory of another directory.
    /// </summary>
    /// <param name="baseDir">The base directory.</param>
    /// <param name="potentialSubDir">The potential subdirectory.</param>
    /// <returns>True if the potential subdirectory is a subdirectory of the base directory, otherwise false.</returns>
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

    /// <summary>
    /// Gets the distance between two paths.
    /// </summary>
    /// <param name="fromPath">The starting path.</param>
    /// <param name="toPath">The target path.</param>
    /// <returns>The distance between the two paths.</returns>
    internal static int GetPathDistance(string fromPath, string toPath)
    {
        var absolut = AbsolutePath.Create(fromPath);
        var relative = absolut.GetRelativePathTo(toPath);
        return relative.ToString().Split(Path.DirectorySeparatorChar).Length - 1;
    }

    #endregion
}

/// <summary>
/// Represents the default configuration settings.
/// </summary>
public class DefaultConfig
{
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; }
    [YamlMember(Alias = "newBranchSchema")] public string NewBranchSchema { get; set; }
    [YamlMember(Alias = "newTagSchema")] public string NewTagSchema { get; set; }
    [YamlMember(Alias = "precision")] public string Precision { get; set; }
    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; }
    [YamlMember(Alias = "release")] public BranchConfig Release { get; set; }
}

/// <summary>
/// Represents the configuration for a specific branch.
/// </summary>
public class BranchConfig
{
    [YamlMember(Alias = "match")] public List<string> Match { get; set; }
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; }
    [YamlMember(Alias = "newBranchSchema")] public string NewBranchSchema { get; set; }
    [YamlMember(Alias = "newTagSchema")] public string NewTagSchema { get; set; }
    [YamlMember(Alias = "precision")] public string Precision { get; set; }
    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; }
}