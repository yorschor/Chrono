using Chrono.Core;
using LibGit2Sharp;
using Microsoft.VisualBasic.CompilerServices;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chrono.Cli.Commands;

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
        throw new NotImplementedException();
    }

    public sealed class Settings : VersionSettings
    {
    }
}

#endregion