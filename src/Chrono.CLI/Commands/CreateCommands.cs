using Chrono.Core;
using Chrono.Core.Helpers;
using Huxy;
using LibGit2Sharp;
using Nuke.Common.Utilities.Collections;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class CreateSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class CreateReleaseBranchCommand : Command<CreateReleaseBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        // 0 Validate Version
        var infoGetResult = VersionInfo.Get();
        if (infoGetResult is IErrorResult)
        {
            settings.Logger.Error(infoGetResult.Message);
            return 0;
        }
        var versionInfo = infoGetResult.Data;
        
        var parseFullVersionResult = versionInfo.GetVersion();
        if (!parseFullVersionResult.Success) return 0;

        var currentBranchConfig = versionInfo.GetConfigForCurrentBranch();

        if (!currentBranchConfig)
        {
            settings.Logger.Error(currentBranchConfig.Message);
            return 0;
        }

        // 1 If working tree is dirty, ask if user wants to continue
        var repoIsDirty = infoGetResult.Data.Repo.RetrieveStatus(new StatusOptions()).Any();
        if (repoIsDirty)
        {
            if (!AnsiConsole.Confirm("Working tree isn't clean. Do you want to continue?"))
            {
                AnsiConsole.MarkupLine("Aborting! No changes have bee made!");
                return 0;
            }
        }

        // 2 Create new branch acording to schema without checking out
        var newBranchName = currentBranchConfig.Data.NewBranchSchema;
        settings.Logger.Info($"Creating new branch {newBranchName}");
        // var branch = infoGetResult.Data.Repo.Branches.Add(newBranchName, infoGetResult.Data.Repo.Head.Tip);

        // 3 Increment Version on existing branch arcording to schema

        // 4 Commit changes of new version if -c | --commit is set
        // if (settings.Commit)
        // {
        //     LibGit2Sharp.Commands.Stage(infoGetResult.Data.Repo, "version.yml");
        //
        //     var author = new Signature("Chrono CLI", "chrono@version.cli", DateTime.Now);
        //
        //     var commit = infoGetResult.Data.Repo.Commit(
        //         "Chrono: Set version. {incrementVersionResult.NewVersion} -> {incrementVersionResult.NewVersion}", author, author);
        //
        //     // settings.Logger.Info("Changes committed with message: Incremented version to " + incrementVersionResult.NewVersion);
        // }

        // infoGetResult.Data.
        return 1;
    }

    public sealed class Settings : CreateSettings
    {
        [CommandOption("-c|--commit")] public bool Commit { get; init; } = false;
    }
}

public class CreateTagCommand : Command<CreateTagCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        return 1;
    }

    public sealed class Settings : CreateSettings
    {
        [CommandArgument(0, "<VERSION>")] public string NewVersion { get; set; }
    }
}

public class CreateBranchCommand : Command<CreateBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        return 1;
    }

    public sealed class Settings : CreateSettings
    {
        [CommandArgument(0, "<BranchKey>")] public string BranchKey { get; set; }
    }
}

#endregion