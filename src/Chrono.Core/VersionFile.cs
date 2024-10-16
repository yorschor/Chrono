using System.Net.Http;
using Huxy;
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

    [YamlMember(Alias = "branches")] public Dictionary<string, BranchConfig> Branches { get; set; } = new();


    /// <summary>
    /// Creates a <see cref="VersionFile"/> instance from the specified YAML file.
    /// </summary>
    /// <param name="path">The path to the YAML file.</param>
    /// <returns>A <see cref="VersionFile"/> instance.</returns>
    public static Result<VersionFile> From(string path)
    {
        return FromAsync(path).GetAwaiter().GetResult();
    }

    public static async Task<Result<VersionFile>> FromAsync(string path)
    {
        var mainYamlContent = File.ReadAllText(path);
        var finalYamlContent = mainYamlContent;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        VersionFile tempVersionFile;
        try
        {
            tempVersionFile = deserializer.Deserialize<VersionFile>(mainYamlContent);
        }
        catch (Exception e)
        {
            return Result.Fail<VersionFile>($"Invalid YAML file! --- ParseError: {e}");
        }

        if (!string.IsNullOrEmpty(tempVersionFile.Default?.InheritFrom))
        {
            var inheritedYamlContentResult = await FetchYamlFromUriAsync(tempVersionFile.Default?.InheritFrom);
            if (inheritedYamlContentResult.Success)
            {
                var inheritedYamlContent = inheritedYamlContentResult.Data;
                finalYamlContent = MergeYamlContent(inheritedYamlContent, mainYamlContent);
            }
            else if (!inheritedYamlContentResult)
            {
                return Result.Fail<VersionFile>(inheritedYamlContentResult);
            }
        }

        var finishedVersionFile = deserializer.Deserialize<VersionFile>(finalYamlContent);

        finishedVersionFile.Branches ??= new Dictionary<string, BranchConfig>();
        finishedVersionFile.Default ??= new DefaultConfig();
        return Result.Ok(deserializer.Deserialize<VersionFile>(finalYamlContent));
    }


    public static async Task<Result<string>> FetchYamlFromUriAsync(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return Result.Fail<string>("URI is not set.");
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri);
            return Result.Ok(response);
        }
        catch (Exception ex)
        {
            return Result.Fail<string>($"Failed to fetch the YAML file: {ex.Message}");
        }
    }

    internal static string MergeYamlContent(string baseYaml, string overrideYaml)
    {
        var deserializer = new DeserializerBuilder().Build();
        var serializer = new SerializerBuilder().Build();

        // Deserialize the base and override YAML strings into dynamic objects
        var baseYamlObject = deserializer.Deserialize(new StringReader(baseYaml));
        var overrideYamlObject = deserializer.Deserialize(new StringReader(overrideYaml));

        var mergedYamlObject = MergeYamlObjects(baseYamlObject, overrideYamlObject);
        var writer = new StringWriter();
        serializer.Serialize(writer, mergedYamlObject);
        return writer.ToString();
    }

    private static object MergeYamlObjects(object baseObj, object overrideObj)
    {
        switch (baseObj)
        {
            case IDictionary<object, object> baseDict when overrideObj is IDictionary<object, object> overrideDict:
            {
                foreach (var key in overrideDict.Keys)
                {
                    var baseValue = baseDict.ContainsKey(key) ? baseDict[key] : null;
                    baseDict[key] = MergeYamlObjects(baseValue, overrideDict[key]);
                }

                return baseDict;
            }
            case IList<object> when overrideObj is IList<object> overrideList:
                return overrideList;
            default:
                return overrideObj ?? baseObj;
        }
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
            return Result.Fail(ex.Message);
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
            return Result.Fail<string>("Directory paths and file name cannot be null or empty!");
        }

        var files = Directory.EnumerateFiles(stopDirectory, targetFileName, SearchOption.AllDirectories);
        var enumerable = files as string[] ?? files.ToArray();

        if (!enumerable.Any())
        {
            return Result.Fail<string>("No version.yml present");
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

#region Models

public class DefaultConfig : BranchConfig
{
    [YamlMember(Alias = "inheritFrom")] public string InheritFrom { get; set; } = "";
    [YamlMember(Alias = "dirtyRepo")] public string DirtyRepo { get; set; } = "dirty-repo";
    [YamlMember(Alias = "release")] public BranchConfig Release { get; set; } = null;
}

public class BranchConfig
{
    [YamlMember(Alias = "match")] public List<string> Match { get; set; } = [];
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; } = "";
    [YamlMember(Alias = "newBranchSchema")] public string NewBranchSchema { get; set; } = "";
    [YamlMember(Alias = "newTagSchema")] public string NewTagSchema { get; set; } = "";
    [YamlMember(Alias = "precision")] public VersionComponent? Precision { get; set; } = VersionComponent.Minor;
    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; } = "";
}

public class BranchConfigWithFallback(BranchConfig defaultConfig, BranchConfig specificConfig)
{
    public List<string> Match => specificConfig.Match ?? defaultConfig.Match;
    public string VersionSchema => specificConfig.VersionSchema ?? defaultConfig.VersionSchema;
    public string NewBranchSchema => specificConfig.NewBranchSchema ?? defaultConfig.NewBranchSchema;
    public string NewTagSchema => specificConfig.NewTagSchema ?? defaultConfig.NewTagSchema;
    public VersionComponent Precision => specificConfig.Precision ?? defaultConfig.Precision ?? VersionComponent.Minor;
    public string PrereleaseTag => specificConfig.PrereleaseTag ?? defaultConfig.PrereleaseTag;
}

#endregion