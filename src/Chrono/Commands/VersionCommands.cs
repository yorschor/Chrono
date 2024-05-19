using Chrono.Core;
using Spectre.Console;
using Spectre.Console.Cli;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class VersionSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class GetVersionCommand : Command<GetVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        const string versionFileName = "version.yml";
        var p = AppContext.BaseDirectory + versionFileName;
        var versionInfo = new VersionInfo(p);
        var parseResult = versionInfo.ParseVersion();
        if (parseResult is not IErrorResult errorResult)
        {
            AnsiConsole.Console.WriteLine(parseResult.Data);
            NLogHelper.EnableShortConsoleTarget();
            return 1;
        }

        {
            NLogHelper.EnableShortConsoleTarget();
            errorResult.PrintAll();
            return 0;
        }
    }

    public sealed class Settings : VersionSettings
    {
    }
}

public class SetVersionCommand : Command<SetVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }
        
        const string versionFileName = "version.yml";
        var p = AppContext.BaseDirectory + versionFileName;
        var versionInfo = new VersionInfo(p);
        var setResult = versionInfo.SetVersion(settings.NewVersion);
        if (setResult is IErrorResult err)
        {
            NLogHelper.EnableShortConsoleTarget();
            AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
            return 0;
        }
        NLogHelper.EnableShortConsoleTarget();
        AnsiConsole.MarkupLine($"[green]Successfully set version to {settings.NewVersion}[/]");
        return 1; 
    }

    public sealed class Settings : VersionSettings
    {

        [CommandArgument(0, "<VERSION>")]
        public string NewVersion { get; set; }
    }
}

#endregion