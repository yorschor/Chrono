using System.Text.RegularExpressions;

namespace Chrono.Core;

public static class RegexPatterns
{
    public static Regex DuplicateBlocksRegex { get; } = new(@"(\[[^\]]*\])(?=\[[^\]]*\])", RegexOptions.Compiled);
    public static Regex EndBlockRegex { get; } = new(@"(\[[^\]]*\])$", RegexOptions.Compiled);
    public static Regex CurlyBracketsRegex { get; } = new(@"\{.*?\}", RegexOptions.Compiled);
    public static Regex BlockContentRegex { get; } = new(@"\{([^\}]*)\}|\[([^\]]*)\]", RegexOptions.Compiled);
    public static Regex ValidVersionRegex { get; } = new(@"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$", RegexOptions.Compiled);
    public static Regex NumericVersionOnlyRegex { get; } = new(@"^\d+\.\d+(?:\.\d+)?(?:\.\d+)?", RegexOptions.Compiled);
    /// <summary>
    /// Matches the top-level "version:" line only (start of line, so nested or foreign keys named "version" are
    /// left alone). Group 1 is the key with its spacing, group 2 the optional quote, group 3 the value. A trailing
    /// comment after the value is not part of the match.
    /// </summary>
    public static Regex VersionYamlTagRegex { get; } = new(@"^(version:[ \t]*)([""']?)([^""'\s#]+)\2", RegexOptions.Compiled | RegexOptions.Multiline);

    public static Regex ValidSemVersionRegex { get; } = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled);
}