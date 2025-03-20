using System.Net.Http;
using Chrono.Core.Helpers;
using Huxy;
using NLog;
using YamlDotNet.Core;
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

        var parseResult = TryParseYaml(mainYamlContent);
        if (!parseResult)
        {
            return parseResult;
        }

        var tempVersionFile = parseResult.Data;

        if (!string.IsNullOrEmpty(tempVersionFile.Default?.InheritFrom))
        {
            var inheritedYamlContentResult = await FetchYamlFromUriAsync(tempVersionFile.Default?.InheritFrom);
            if (inheritedYamlContentResult.Success)
            {
                var inheritedYamlContent = inheritedYamlContentResult.Data;
                parseResult = TryParseYaml(inheritedYamlContent, "remote version file");
                if (!parseResult)
                {
                    return parseResult;
                }

                finalYamlContent = YamlHelper.MergeYamlContent(inheritedYamlContent, mainYamlContent);
            }
            else if (!inheritedYamlContentResult)
            {
                return Result.Fail<VersionFile>(inheritedYamlContentResult);
            }
        }

        parseResult = TryParseYaml(finalYamlContent, "merged version file");
        if (!parseResult)
        {
            return parseResult;
        }

        var finishedVersionFile = parseResult.Data;

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

        if (uri.StartsWith("file://"))
        {
            try
            {
                var filePath = uri.Substring(7);
                filePath = Path.GetFullPath(filePath);
                if (!File.Exists(filePath))
                {
                    return Result.Fail<string>($"File not found at {filePath}");
                }
                var fileContent = File.ReadAllText(filePath);
                return Result.Ok(fileContent);
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Failed to read file from {uri}: {ex.Message}");
            }
        }
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri);
            return Result.Ok(response);
        }
        catch (Exception ex)
        {
            return Result.Fail<string>($"Failed to fetch version file from {uri} to inherit from: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the version key safely in the YAML file and saves it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Result UpdateVersionInFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);

            if (!RegexPatterns.VersionYamlTagRegex.IsMatch(text))
                return Result.Fail(
                    "Version key not found in YAML file. If you see this... Something major broke. Please file a new issue at github.com/yorschor/chrono");

            var newText = RegexPatterns.VersionYamlTagRegex.Replace(text, m =>
            {
                var quote = m.Groups[1].Value;
                return $"version: {quote}{Version}{quote}";
            });

            File.WriteAllText(path, newText);
            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
    }
    
    #region Helpers

    private static Result<VersionFile>TryParseYaml(string yamlContent, string fileName = "local version file")
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        try
        {
            return Result.Ok(deserializer.Deserialize<VersionFile>(yamlContent));
        }
        catch (YamlException e)
        {
            return Result.Fail<VersionFile>(YamlParsingException.FromYamlException(e, yamlContent, fileName));
        }
        catch (Exception e)
        {
            return Result.Fail<VersionFile>($"Invalid YAML file! --- ParseError: {e.Message}", e);
        }
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
    [YamlMember(Alias = "match")] public List<string> Match { get; set; }
    [YamlMember(Alias = "versionSchema")] public string VersionSchema { get; set; }

    [YamlMember(Alias = "newBranchSchema")]
    public string NewBranchSchema { get; set; }

    [YamlMember(Alias = "newTagSchema")] public string NewTagSchema { get; set; }
    [YamlMember(Alias = "precision")] public VersionComponent? Precision { get; set; }
    [YamlMember(Alias = "prereleaseTag")] public string PrereleaseTag { get; set; }
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