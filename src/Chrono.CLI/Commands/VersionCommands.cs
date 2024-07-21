using Chrono.Core;
using Chrono.Core.Helpers;
using Huxy;
using Nuke.Common.Utilities.Collections;
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
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        // var repoFoundResult = GitUtil.GetRepoRootPath();
        // if (repoFoundResult is IErrorResult)
        // {
        //     repoFoundResult.PrintErrors();
        //     return 0;
        // }
        //
        // var versionFileFoundResult = VersionFile.Find(
        //     Directory.GetCurrentDirectory(),
        //     repoFoundResult.Data);
        //
        // if (versionFileFoundResult is IErrorResult)
        // {
        //     repoFoundResult.PrintErrors();
        //     return 1;
        // }

        var versionInfoResult = VersionInfo.Get();
        if (!versionInfoResult)
        {
            versionInfoResult.PrintErrors();
            return 0;
        }

        var versionInfo = versionInfoResult.Data;
        if (settings.Debug)
        {
            var tree = new Tree($"VersionInfo for [gray]{versionInfo}[/]");
            tree.AddNode($"Major: {versionInfo.Major}");
            tree.AddNode($"Minor: {versionInfo.Minor}");
            tree.AddNode($"Patch: {versionInfo.Patch}");
            tree.AddNode($"Build: {versionInfo.Build}");
            tree.AddNode($"PrereleaseTag: {versionInfo.PrereleaseTag}");
            tree.AddNode($"CommitShortHash: {versionInfo.CommitShortHash}");
            tree.AddNode($"BranchName: {versionInfo.BranchName}");
            tree.AddNode("Tags").AddNodes(versionInfo.TagNames);
            tree.AddNode("SearchArray").AddNodes(versionInfo.CombinedSearchArray);
            AnsiConsole.Write(tree);
        }

        var parseResult = settings.Numeric ? versionInfo.GetNumericVersion() : versionInfo.GetVersion();
        if (parseResult is IErrorResult)
        {
            parseResult.PrintErrors();
            return 0;
        }

        AnsiConsole.Console.WriteLine(parseResult.Data);
        NLogHelper.EnableShortConsoleTarget();
        return 1;
    }

    public sealed class Settings : VersionSettings
    {
        [CommandOption("-n|--numeric")] public bool Numeric { get; init; } = false;
    }
}

public class SetVersionCommand : Command<SetVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        var versionInfoResult = VersionInfo.Get();
        if (!versionInfoResult)
        {
            versionInfoResult.PrintErrors();
            return 0;
        }
        var versionInfo = versionInfoResult.Data;
        
        var setResult = versionInfo.SetVersion(settings.NewVersion);
        if (setResult is IErrorResult)
        {
            NLogHelper.EnableShortConsoleTarget();
            AnsiConsole.MarkupLine($"[red]Error: {setResult.Message}[/]");
            return 0;
        }

        NLogHelper.EnableShortConsoleTarget();
        AnsiConsole.MarkupLine($"[green]Successfully set version to {settings.NewVersion}[/]");
        return 1;
    }

    public sealed class Settings : VersionSettings
    {
        [CommandArgument(0, "<VERSION>")] public string NewVersion { get; set; }
    }
}

public class BumpVersionCommand : Command<BumpVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        var versionComponent = settings.VersionComponent switch
        {
            "major" => VersionComponent.Major,
            "minor" => VersionComponent.Minor,
            "patch" => VersionComponent.Patch,
            "build" => VersionComponent.Build,
            _ => VersionComponent.INVALID
        };
        var infoRes = VersionInfo.Get();
        var res = infoRes.Data.BumpVersion(versionComponent);
        if (res is IErrorResult)
        {
            NLogHelper.EnableShortConsoleTarget();
            AnsiConsole.MarkupLine($"[red]Error: {res.Message}[/]");
            return 0;
        }

        NLogHelper.EnableShortConsoleTarget();
        AnsiConsole.MarkupLine($"[green]Successfully set version to {infoRes.Data.GetVersion().Data}[/]");
        return 1;
    }

    public sealed class Settings : VersionSettings
    {
        [CommandArgument(0, "<Version Component>")]
        public string VersionComponent { get; set; }
    }
}

#endregion