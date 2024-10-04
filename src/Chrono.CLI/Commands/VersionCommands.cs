using Chrono.Core;
using Chrono.Core.Helpers;
using Huxy;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class VersionSettings : BaseCommandSettings;

#endregion

#region Commands

public class GetVersionCommand : Command<GetVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        NLogHelper.SetLogLevel(settings.Trace);
        var versionInfoResult = VersionInfo.Get(settings.IgnoreDirty);
        if (!versionInfoResult)
        {
            versionInfoResult.PrintErrors();
            return 1;
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
        if (!parseResult)
        {
            parseResult.PrintErrors();
            return 1;
        }

        AnsiConsole.Console.WriteLine(parseResult.Data);
        NLogHelper.SetLogLevel(false);
        return 0;
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
        NLogHelper.SetLogLevel(settings.Trace);
        var versionInfo = settings.ValidateVersionInfo();
        if (versionInfo is null) return settings.GetReturnCode(1);
        
        var setResult = versionInfo.SetVersion(settings.NewVersion);
        if (!setResult)
        {
            NLogHelper.SetLogLevel(false);
            AnsiConsole.MarkupLine($"[red]Error: {setResult.Message}[/]");
            return 1;
        }

        NLogHelper.SetLogLevel(false);
        AnsiConsole.MarkupLine($"[green]Successfully set version to {settings.NewVersion}[/]");
        return 0;
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
        NLogHelper.SetLogLevel(settings.Trace);

        var versionComponent = settings.VersionComponent switch
        {
            "major" => VersionComponent.Major,
            "minor" => VersionComponent.Minor,
            "patch" => VersionComponent.Patch,
            "build" => VersionComponent.Build,
            _ => VersionComponent.INVALID
        };
        var versionInfo = settings.ValidateVersionInfo();
        if (versionInfo is null) return settings.GetReturnCode(1);
        var res = versionInfo.BumpVersion(versionComponent);
        if (!res)
        {
            NLogHelper.SetLogLevel(false);
            AnsiConsole.MarkupLine($"[red]Error: {res.Message}[/]");
            return 1;
        }

        NLogHelper.SetLogLevel(false);
        AnsiConsole.MarkupLine($"[green]Successfully set version to {versionInfo.GetVersion().Data}[/]");
        return 0;
    }

    public sealed class Settings : VersionSettings
    {
        [CommandArgument(0, "<Version Component>")]
        public string VersionComponent { get; set; }
    }
}

#endregion