using Chrono.Core.Helpers;
using NLog;
using Nuke.Common.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Chrono.Core;

public class VersionFile
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [YamlMember(Alias = "version")] public string Version { get; set; }

    [YamlMember(Alias = "default")] public DefaultConfig Default { get; set; }

    [YamlMember(Alias = "branches")] public Dictionary<string, BranchConfig> Branches { get; set; }

    public static VersionFile From(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlContent = File.ReadAllText(path);
        return deserializer.Deserialize<VersionFile>(yamlContent);
    }

    public Result Save(string path)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlContent = serializer.Serialize(this);
            File.WriteAllText(path, yamlContent);

            return new SuccessResult();
        }
        catch (Exception ex)
        {
            return new ErrorResult(ex.Message);
        }
    }
    
    public static Result<string> Find(string startDirectory, string stopDirectory, string targetFileName = "version.yml")
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
            if (!VersionFile.IsSubdirectory(startDirectory, Path.GetDirectoryName(enumerable[0])))
            {
                return new SuccessResult<string>(enumerable[0]);
            }

            return new ErrorResult<string>("The file is in a subdirectory of the start directory.");
        }


        string nearestFile = null;
        var minDistance = int.MaxValue;

        foreach (var file in enumerable)
        {
            if (VersionFile.IsSubdirectory(startDirectory, Path.GetDirectoryName(file)))
            {
                continue;
            }

            var distance = VersionFile.GetPathDistance(startDirectory, file);
            Logger.Trace(distance);
            if (distance < 0 || distance >= minDistance) continue;
            minDistance = distance;
            nearestFile = file;
        }

        return nearestFile != null
            ? new SuccessResult<string>(nearestFile)
            : new ErrorResult<string>("Something went wrong while searching for version.yml");
    }

    #region Helpers

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
        var relative = absolut.GetRelativePathTo(toPath);
        return relative.ToString().Split(Path.DirectorySeparatorChar).Length - 1;
    }

    #endregion
}

public class DefaultConfig
{
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; }

    [YamlMember(Alias = "precision")] public string Precision { get; set; }

    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; }
    [YamlMember(Alias = "release")] public BranchConfig Release { get; set; }
}

public class BranchConfig
{
    [YamlMember(Alias = "match")] public List<string> Match { get; set; }
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; }

    [YamlMember(Alias = "precision")] public string Precision { get; set; }

    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; }
}