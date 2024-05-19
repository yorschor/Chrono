using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Chrono.Core;

public class VersionFileModel
{
    [YamlMember(Alias = "version")] public string Version { get; set; }

    [YamlMember(Alias = "default")] public DefaultConfig Default { get; set; }

    [YamlMember(Alias = "branches")] public Dictionary<string, BranchConfig> Branches { get; set; }

    public static VersionFileModel From(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlContent = File.ReadAllText(path);
        return deserializer.Deserialize<VersionFileModel>(yamlContent);
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