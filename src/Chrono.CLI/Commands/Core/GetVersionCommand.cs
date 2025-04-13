using Chrono.Core;
using Chrono.Core.Helpers;
using Chrono.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chrono.Commands.Core;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global
public class GetVersionCommand : Command<GetVersionCommand.Settings>
{
    public sealed class Settings : BaseCommandSettings
    {
        [CommandOption("-n|--numeric")] public bool Numeric { get; init; } = false;
        [CommandOption("-e|--useEnvVars")] public bool UseEnvVars { get; init; } = false;

        [CommandOption("-p|--gitRoot")] public string RootPath { get; set; } = "";
        [CommandArgument(0, "[Target]")] public string TargetVersionFile { get; set; } = "";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        NLogHelper.SetLogLevel(settings.Trace);
        var versionInfoResult = VersionInfo.Get(settings.IgnoreDirty, settings.UseEnvVars, settings.RootPath, settings.TargetVersionFile);
        if (!versionInfoResult)
        {
            versionInfoResult.PrintFailures();
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
            tree.AddNode($"PrereleaseTag: {versionInfo.CurrentBranchConfig.PrereleaseTag}");
            tree.AddNode($"CommitShortHash: {versionInfo.GitInfoProvider.CommitShortHash}");
            tree.AddNode($"BranchName: {versionInfo.GitInfoProvider.BranchName}");
            // tree.AddNode("Tags").AddNodes(versionInfo.GitInfoProvider.TagNames);
            AnsiConsole.Write(tree);
        }

        var parseResult = settings.Numeric ? versionInfo.GetNumericVersion() : versionInfo.GetVersion();
        if (!parseResult)
        {
            parseResult.PrintFailures();
            return 1;
        }

        AnsiConsole.Console.WriteLine(parseResult.Data);
        NLogHelper.SetLogLevel(false);
        return 0;
    }
}