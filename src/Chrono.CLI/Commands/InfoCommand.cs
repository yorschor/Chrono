using Chrono.Core;
using Chrono.Core.Helpers;
using Chrono.Helpers;
using Huxy;
using LibGit2Sharp;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class InfoSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class GetInfoCommand : Command<GetInfoCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        // if (settings.Trace)
        // {
        //     NLogHelper.EnableShortConsoleTarget(true);
        // }
        //
        // var currentDir = Directory.GetCurrentDirectory();
        // settings.Logger.Trace($"Current dir: {currentDir}");
        // var repoRootResult = settings.GetRepo();
        // if (!repoRootResult.Success)
        // {
        //     return 0;
        // }
        // var result = VersionFile.Find(currentDir, repoRootResult.Data.Info.WorkingDirectory);
        //
        // if (result is not IErrorResult)
        // {
        //     AnsiConsole.MarkupLine(result.Data);
        //     return 1;
        // }
        //
        // NLogHelper.EnableShortConsoleTarget();
        // AnsiConsole.MarkupLine("Could not find any version.yml for " + currentDir);
        
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left")
                    .SplitRows(
                        new Layout("Header"),
                        new Layout("InfoPanel")
                        ),
                new Layout("Right")
                    .SplitRows(
                        new Layout("Logo"),
                        new Layout("Bottom")));
        var rootPanel = new Panel(layout)
        {
            Border = BoxBorder.None
        };
        layout["Header"].Update(new Panel(new FigletText("Chrono")));
        layout["Logo"].Update(new Panel(AsciiLogo.Get()));
        
        AnsiConsole.Write(rootPanel);
        return 0;
    }

    public sealed class Settings : InfoSettings
    {
    }
}

#endregion