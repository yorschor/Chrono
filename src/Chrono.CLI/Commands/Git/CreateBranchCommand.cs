using Chrono.Core.Helpers;
using Chrono.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands.Git;

#region Commands

public class CreateBranchCommand : Command<CreateBranchCommand.Settings>
{
    public sealed class Settings : BaseCommandSettings;

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            NLogHelper.SetLogLevel(settings.Trace);
            var versionInfo = settings.ValidateVersionInfo();
            if (versionInfo is null) return settings.GetReturnCode(1);
            var repoResult = settings.GetRepo();
            if (!repoResult) return 1;

            var newBranchNameResult = versionInfo.ResolveSchema(versionInfo.CurrentBranchConfig.NewBranchSchema);
            if (!newBranchNameResult)
            {
                newBranchNameResult.PrintFailures();
                NLogHelper.SetLogLevel(false);
                AnsiConsole.MarkupLine(newBranchNameResult.Message);
                return 1;
            }

            NLogHelper.SetLogLevel(false);
            settings.MarkupAndTrace($"Creating new branch {newBranchNameResult.Data}");
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
}

#endregion