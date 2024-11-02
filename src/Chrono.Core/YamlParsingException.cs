using YamlDotNet.Core;

namespace Chrono.Core;

public class YamlParsingException : Exception
{
    public string FileName { get; }
    public int FileLineLength { get; }
    public Mark Start { get; }
    public Mark End { get; }
    public string[] SurroundingLines { get; }

    private YamlParsingException(string fileName, int fileLineLength, string message, Mark start, Mark end, string[] surroundingLines, Exception innerException)
        : base(message, innerException)
    {
        FileName = fileName;
        FileLineLength = fileLineLength;
        Start = start;
        End = end;
        SurroundingLines = surroundingLines;
    }

    public static YamlParsingException FromYamlException(YamlException e, string yamlContent, string fileName)
    {
        var errorLine = e.Start.Line;
        var errorColumn = e.Start.Column;

        var fileAsLines = yamlContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var surroundingLines = GetSurroundingLines(fileAsLines, errorLine - 1);
        var message = $"YAML parsing error for {fileName} at line {errorLine}, column {errorColumn}: {e.Message}";

        return new YamlParsingException(fileName, fileAsLines.Length, message, e.Start, e.End, surroundingLines, e);
    }

    private static string[] GetSurroundingLines(string[] lines, long errorIndex)
    {
        var surroundingLines = new List<string>();

        // Line before the error
        if (errorIndex - 1 >= 0)
            surroundingLines.Add(lines[errorIndex - 1]);
        else
            surroundingLines.Add("");

        // Error line
        if (errorIndex >= 0 && errorIndex < lines.Length)
            surroundingLines.Add(lines[errorIndex]);
        else
            surroundingLines.Add("Something went wrong here");
        
        // Line after the error
        if (errorIndex + 1 < lines.Length)
            surroundingLines.Add(lines[errorIndex + 1]);
        else
            surroundingLines.Add("");
        
        return surroundingLines.ToArray();
    }
}