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
        AnsiConsole.MarkupLine(
            $"YAML parsing error for [underline grey93]{e.FileName}[/] at line [grey93]{e.ErrorLine}[/], column [grey93]{e.ErrorColumn}[/]");
        AnsiConsole.MarkupLine("");
        for (var i = 0; i < e.SurroundingLines.Length; i++)
        {
            AnsiConsole.MarkupLine(e.SurroundingLines[i]);
            if (i == e.SurroundingLines.Length - 2)
                AnsiConsole.MarkupLine(new string(' ', e.ErrorColumn - 1) + "[RED]^[/]");
        }
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine(e.Message);
    }
}