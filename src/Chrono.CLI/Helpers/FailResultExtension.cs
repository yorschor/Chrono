using Huxy;
using NLog;
using Spectre.Console;

namespace Chrono.Helpers;

public static class ResultExtension
{
    public static void PrintFailures(this IResult errorResult)
    {
        var logger = LogManager.GetCurrentClassLogger();
        if (!string.IsNullOrEmpty(errorResult.Message))
        {
            AnsiConsole.WriteLine(errorResult.Message);
        }
        if (logger.IsTraceEnabled && errorResult.Exception is not null)
        {
            AnsiConsole.WriteException(errorResult.Exception);
        }
    }
}