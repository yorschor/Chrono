using Chrono.Core;
using Huxy;
using NLog;
using Spectre.Console;
using YamlDotNet.Core;

namespace Chrono.Helpers;

public static class ResultExtension
{
    public static void PrintFailures(this IResult errorResult)
    {
        var logger = LogManager.GetCurrentClassLogger();
        if (errorResult.Exception is YamlParsingException e)
        {
            PrintYamlException(e);
        }
        else
        {
            if (!string.IsNullOrEmpty(errorResult.Message))
            {
                AnsiConsole.WriteLine(errorResult.Message);
            }
        }

        if (logger.IsTraceEnabled && errorResult.Exception is not null)
        {
            AnsiConsole.WriteException(errorResult.Exception);
        }
    }

    private static void PrintYamlException(YamlParsingException e)
    {
        var omittedLinesAbove = e.Start.Line - e.SurroundingLines.Length;
        var omittedLinesBelow = e.FileLineLength - e.Start.Line - (e.SurroundingLines.Length - 1);

        AnsiConsole.MarkupLine(
            $"YAML parsing error for [underline grey93]{e.FileName}[/] at line [grey93]{e.Start.Line}[/], column [grey93]{e.Start.Column}[/]");
        AnsiConsole.WriteLine("");
        if (e.Start.Line - 1 >= 0)
        {
            if (e.Start.Column - 1 != 0)
            {
                AnsiConsole.MarkupLine($"...");
                AnsiConsole.WriteLine("");
            }

            AnsiConsole.MarkupLine($"[grey70]{e.Start.Line-1}[/] {e.SurroundingLines[0]}");
        }

        var errorLine = e.SurroundingLines[1];
        errorLine = errorLine.Insert(e.End.Column - 1, "[/]");
        errorLine = errorLine.Insert(e.Start.Column - 1, "[RED]");
        var lineNumberString = $"{e.Start.Line} ";
        AnsiConsole.MarkupLine($"[grey70]{lineNumberString}[/]{errorLine}");
        AnsiConsole.MarkupLine(new string(' ', e.Start.Column + lineNumberString.Length - 1) + "[RED]^[/]");


        if (e.Start.Line + 1 <= e.FileLineLength)
        {
            AnsiConsole.MarkupLine($"[grey70]{e.Start.Line+1}[/] {e.SurroundingLines[2]}");
            
            if (e.Start.Column - 1 != e.FileLineLength)
            {
                AnsiConsole.WriteLine("");
                AnsiConsole.MarkupLine($"... +{omittedLinesBelow} lines");
            }
        }

        AnsiConsole.WriteLine("");
        AnsiConsole.MarkupLine("[grey70]Full error message:[/]");
        AnsiConsole.MarkupLine(e.Message);
    }
}