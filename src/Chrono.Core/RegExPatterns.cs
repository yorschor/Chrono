using System.Text.RegularExpressions;

namespace Chrono.Core;

public static class RegexPatterns
{
    private static readonly Regex _duplicateBlocksRegex = new Regex(@"(\[[^\]]*\])(?=\[[^\]]*\])", RegexOptions.Compiled);
    private static readonly Regex _endBlockRegex = new Regex(@"(\[[^\]]*\])$", RegexOptions.Compiled);
    private static readonly Regex _blockContentRegex = new Regex(@"\{([^\}]*)\}|\[([^\]]*)\]", RegexOptions.Compiled);
    private static readonly Regex _validVersionRegex = new Regex(@"^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$", RegexOptions.Compiled);
    private static readonly Regex _validSemVersionRegex = new Regex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.Compiled);

    public static Regex DuplicateBlocksRegex => _duplicateBlocksRegex;
    public static Regex EndBlockRegex => _endBlockRegex;
    public static Regex BlockContentRegex => _blockContentRegex;
    public static Regex ValidVersionRegex => _validVersionRegex;
    public static Regex ValidSemVersionRegex => _validSemVersionRegex;
}