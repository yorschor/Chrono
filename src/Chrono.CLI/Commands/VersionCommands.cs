using Chrono.Core;
using Chrono.Core.Helpers;
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

        var repoFoundResult = GitUtil.GetRepoRootPath();
        if (repoFoundResult is IErrorResult repoErr)
        {
            repoErr.PrintAll();
            return 0;
        }

        var versionFileFoundResult = VersionFile.Find(
            Directory.GetCurrentDirectory(),
            repoFoundResult.Data);

        if (versionFileFoundResult is IErrorResult verErr)
        {
            verErr.PrintAll();
            return 1;
        }

        var versionInfo = new VersionInfo(versionFileFoundResult.Data);
        if (settings.Debug)
        {
            var tree = new Tree($"VersionInfo for [gray]{versionFileFoundResult.Data}[/]");
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

        var parseResult = settings.Numeric ? versionInfo.GetNumericVersion() : versionInfo.ParseVersion();
        if (parseResult is IErrorResult parseErr)
        {
            parseErr.PrintAll();
            return 0;
        }
        AnsiConsole.Console.WriteLine(parseResult.Data);
        NLogHelper.EnableShortConsoleTarget();
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
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        const string versionFileName = "version.yml";
        var p = Path.Combine(Environment.CurrentDirectory, versionFileName);
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
        var res = VersionInfo.Get().Data.BumpVersion(versionComponent);
        if (res is IErrorResult err)
        {
            NLogHelper.EnableShortConsoleTarget();
            AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
            return 0;
        }

        NLogHelper.EnableShortConsoleTarget();
        AnsiConsole.MarkupLine($"[green]Successfully set version to [/]");
        return 1;
    }

    public sealed class Settings : VersionSettings
    {
        [CommandArgument(0, "<Version Component>")]
        public string VersionComponent { get; set; }
    }
}

#endregion