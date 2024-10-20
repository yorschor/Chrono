using Chrono.Core;
using Chrono.Core.Helpers;
using Chrono.Helpers;
using LibGit2Sharp;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class CreateSettings : BaseCommandSettings;

#endregion

#region Commands

public class CreateReleaseBranchCommand : Command<CreateReleaseBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        NLogHelper.SetLogLevel(settings.Trace);
        var versionInfo = settings.ValidateVersionInfo();
        if (versionInfo is null) return settings.GetReturnCode(1);

        // 1 Create new branch according to schema without checking out
        var newBranchNameResult = versionInfo.GetNewBranchName(true);

        if (!newBranchNameResult)
        {
            newBranchNameResult.PrintFailures();
            return 1;
        }

        var repo = settings.GetRepo().Data;
        settings.Logger.Trace($"Creating new branch {newBranchNameResult.Data}");
        AnsiConsole.MarkupLine($"Creating new branch {newBranchNameResult.Data}");
        var branch = repo.Branches.Add(newBranchNameResult.Data, repo.Head.Tip);

        // 2 Increment Version on existing branch according to schema
        var oldversion = versionInfo.GetNumericVersion();
        versionInfo.BumpVersion(versionInfo.CurrentBranchConfig.Precision);
        var newVersion = versionInfo.GetNumericVersion();

        // 3 Commit changes of new version if -c | --commit is set
        if (settings.Commit)
        {
            LibGit2Sharp.Commands.Stage(repo, "version.yml");

            var signature = repo.Config.BuildSignature(DateTimeOffset.Now)
                            ?? new Signature("Chrono CLI", "chrono@version.cli", DateTime.Now);
            var author = new Signature(signature.Name, signature.Email, signature.When);

            var commit = repo.Commit(settings.CommitMessage.Replace("{oldVersion}", oldversion.Data).Replace("{newVersion}", newVersion.Data), author,
                author);
        }
        NLogHelper.SetLogLevel(false);
        return 0;
    }

    public sealed class Settings : CreateSettings
    {
        [CommandOption("-c|--commit")] public bool Commit { get; init; } = false;
        
        [CommandArgument(1, "[Commit Message {oldVersion} {newVersion}]")]
        public string CommitMessage { get; init; } = "Chrono: Set version {oldVersion} => {newVersion}";
    }
}

public class CreateTagCommand : Command<CreateTagCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            NLogHelper.SetLogLevel(settings.Trace);
            var versionInfo = settings.ValidateVersionInfo();
            if (versionInfo is null) return settings.GetReturnCode(1);

            var newTagNameResult = versionInfo.GetNewTagName();

            if (!newTagNameResult)
            {
                newTagNameResult.PrintFailures();
                AnsiConsole.MarkupLine(newTagNameResult.Message);
                return 1;
            }

            var repo = settings.GetRepo().Data;
            var tag = repo.Tags.Add(newTagNameResult.Data, repo.Head.Tip);
            NLogHelper.SetLogLevel(false);
            AnsiConsole.MarkupLine($"Tag {newTagNameResult.Data} created");
            return 0;
        }
        catch (Exception e)
        {
            settings.Logger.Error(e.ToString());
            NLogHelper.SetLogLevel(false);
            return 1;
        }
    }

    public sealed class Settings : CreateSettings;
}

public class CreateBranchCommand : Command<CreateBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            NLogHelper.SetLogLevel(settings.Trace);
            var versionInfo = settings.ValidateVersionInfo();
            if (versionInfo is null) return settings.GetReturnCode(1);
            var repoResult = settings.GetRepo();
            if (!repoResult) return 1;

            if (string.IsNullOrEmpty(settings.BranchKey))
            {
                settings.BranchKey = repoResult.Data.Head.FriendlyName;
            }

            var newBranchNameResult = versionInfo.GetNewBranchNameFromKey(settings.BranchKey);
            if (!newBranchNameResult)
            {
                newBranchNameResult.PrintFailures();
                NLogHelper.SetLogLevel(false);
                AnsiConsole.MarkupLine(newBranchNameResult.Message);
                return 1;
            }
            NLogHelper.SetLogLevel(false);
            AnsiConsole.MarkupLine($"Creating new branch {newBranchNameResult.Data}");
            repoResult.Data.Branches.Add(newBranchNameResult.Data, repoResult.Data.Head.Tip);
            return 0;
        }
        catch (Exception e)
        {
            settings.Logger.Error(e.ToString());
            NLogHelper.SetLogLevel(false);
            return 1;
        }
    }

    public sealed class Settings : CreateSettings
    {
        [CommandArgument(0, "[BranchKey]")] public string BranchKey { get; set; }
    }
}

#endregion