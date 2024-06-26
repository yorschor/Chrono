﻿using Chrono.Core;
using Chrono.Core.Helpers;
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
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        var currentDir = Directory.GetCurrentDirectory();
        settings.Logger.Trace($"Current dir: {currentDir}");
        var repoRootResult = GitUtil.GetRepoRootPath();
        if (!repoRootResult.Success)
        {
            return 0;
        }
        var result = VersionFile.Find(currentDir, repoRootResult.Data);

        if (result is not IErrorResult)
        {
            AnsiConsole.MarkupLine(result.Data);
            return 1;
        }
       
        NLogHelper.EnableShortConsoleTarget();
        AnsiConsole.MarkupLine("Could not find any version.yml for " + currentDir);
        return 0;
    }

    public sealed class Settings : InfoSettings
    {
    }
}

#endregion