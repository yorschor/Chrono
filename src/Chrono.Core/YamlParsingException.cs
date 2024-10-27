using YamlDotNet.Core;

namespace Chrono.Core;

public class YamlParsingException : Exception
{
    public string FileName { get; }
    public int ErrorLine { get; }
    public int ErrorColumn { get; }
    public string[] SurroundingLines { get; }

    private YamlParsingException(String fileName,string message, int errorLine, int errorColumn, string[] surroundingLines, Exception innerException)
        : base(message, innerException)
    {
        FileName = fileName;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        SurroundingLines = surroundingLines;
    }

    public static YamlParsingException FromYamlException(YamlException e, string yamlContent, string fileName)
    {
        var errorLine = e.Start.Line;
        var errorColumn = e.Start.Column;

        var fileAsLines = yamlContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var surroundingLines = GetSurroundingLines(fileAsLines, errorLine - 1);
        var message = $"YAML parsing error for {fileName} at line {errorLine}, column {errorColumn}: {e.Message}";

        return new YamlParsingException(fileName, message, errorLine, errorColumn, surroundingLines, e);
    }

    private static string[] GetSurroundingLines(string[] lines, int errorIndex)
    {
        var surroundingLines = new List<string>();

        // Line before the error
        if (errorIndex - 1 >= 0)
            surroundingLines.Add(lines[errorIndex - 1]);

        // Error line
        if (errorIndex >= 0 && errorIndex < lines.Length)
            surroundingLines.Add(lines[errorIndex]);

        // Line after the error
        if (errorIndex + 1 < lines.Length)
            surroundingLines.Add(lines[errorIndex + 1]);

        return surroundingLines.ToArray();
    }
}