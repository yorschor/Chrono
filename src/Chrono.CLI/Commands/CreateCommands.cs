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
    internal VersionInfo? ValidateVersionInfo()
    {
        // 0 Validate Version
        var infoGetResult = VersionInfo.Get();
        if (infoGetResult is IErrorResult)
        {
            Logger.Error(infoGetResult.Message);
            return null;
        }

        var versionInfo = infoGetResult.Data;
        if (!versionInfo.GetVersion()) return null;

        //If working tree is dirty, ask if user wants to continue
        var repoIsDirty = GetRepo().Data.RetrieveStatus(new StatusOptions()).Any();
        if (repoIsDirty)
        {
            if (!AnsiConsole.Confirm("Working tree isn't clean. Do you want to continue?"))
            {
                AnsiConsole.MarkupLine("Aborting! No changes have bee made!");
                return null;
            }
        }

        return versionInfo;
    }
}

#endregion

#region Commands

public class CreateReleaseBranchCommand : Command<CreateReleaseBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var versionInfo = settings.ValidateVersionInfo();
        if (versionInfo is null) return 0;

        // 1 Create new branch according to schema without checking out
        var newBranchNameResult = versionInfo.GetNewBranchName(true);

        if (!newBranchNameResult)
        {
            newBranchNameResult.PrintErrors();
            AnsiConsole.MarkupLine(newBranchNameResult.Message);
            return 0;
        }
        var repo = settings.GetRepo().Data;
        settings.Logger.Trace($"Creating new branch {newBranchNameResult.Data}");
        AnsiConsole.MarkupLine($"Creating new branch {newBranchNameResult.Data}");
        var branch = repo.Branches.Add(newBranchNameResult.Data, repo.Head.Tip);

        // 2 Increment Version on existing branch arcording to schema
        var oldversion = versionInfo.GetNumericVersion();
        versionInfo.BumpVersion(VersionInfo.VersionComponentFromString(versionInfo.CurrentBranchConfig.Precision));
        var newVersion = versionInfo.GetNumericVersion();

        // 3 Commit changes of new version if -c | --commit is set
        if (settings.Commit)
        {
            LibGit2Sharp.Commands.Stage(repo, "version.yml");

            var author = new Signature("Chrono CLI", "chrono@version.cli", DateTime.Now);

            var commit = repo.Commit(
                $"Chrono: Set version. {oldversion.Data} => {newVersion.Data}", author, author);
        }

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
        try
        {
            var versionInfo = settings.ValidateVersionInfo();
            if (versionInfo is null) return 0;

            var newTagNameResult = versionInfo.GetNewTagName();

            if (!newTagNameResult)
            {
                newTagNameResult.PrintErrors();
                AnsiConsole.MarkupLine(newTagNameResult.Message);
                return 0;
            }
            var repo = settings.GetRepo().Data;
            var tag = repo.Tags.Add(newTagNameResult.Data, repo.Head.Tip);
            AnsiConsole.MarkupLine($"Tag {newTagNameResult.Data} created");
            return 1;
        }
        catch (Exception e)
        {
            settings.Logger.Error(e.ToString());
            return 0;
        }
    }

    public sealed class Settings : CreateSettings
    {
    }
}

public class CreateBranchCommand : Command<CreateBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var versionInfo = settings.ValidateVersionInfo();
            if (versionInfo is null) return 0;

            var newBranchNameResult = versionInfo.GetNewBranchNameFromKey(settings.BranchKey);
            if (!newBranchNameResult)
            {
                newBranchNameResult.PrintErrors();
                AnsiConsole.MarkupLine(newBranchNameResult.Message);
                return 0;
            }
            var repo = settings.GetRepo().Data;
            
            AnsiConsole.MarkupLine($"Creating new branch {newBranchNameResult.Data}");
            repo.Branches.Add(newBranchNameResult.Data, repo.Head.Tip);
            return 1;
        }
        catch (Exception e)
        {
            settings.Logger.Error(e.ToString());
            return 0;
        }
    }

    public sealed class Settings : CreateSettings
    {
        [CommandArgument(0, "<BranchKey>")] public string BranchKey { get; set; }
    }
}

#endregion