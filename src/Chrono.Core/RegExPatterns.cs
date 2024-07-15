using System.Text.RegularExpressions;

namespace Chrono.Core;

public static class RegexPatterns
{
    public static Regex DuplicateBlocksRegex { get; } = new(@"(\[[^\]]*\])(?=\[[^\]]*\])", RegexOptions.Compiled);
    public static Regex EndBlockRegex { get; } = new(@"(\[[^\]]*\])$", RegexOptions.Compiled);
    public static Regex BlockContentRegex { get; } = new(@"\{([^\}]*)\}|\[([^\]]*)\]", RegexOptions.Compiled);
    public static Regex ValidVersionRegex { get; } = new(@"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$", RegexOptions.Compiled);
    public static Regex NumericVersionOnlyRegex { get; } = new(@"^\d+\.\d+(?:\.\d+)?(?:\.\d+)?", RegexOptions.Compiled);
    public static Regex ValidSemVersionRegex { get; } = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.Compiled);
}