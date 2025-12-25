using Chrono.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chrono.Commands.Git;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global
public class CreateTagCommand : Command<CreateTagCommand.Settings>
{
	public sealed class Settings : BaseCommandSettings;

	public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		try
		{
			NLogHelper.SetLogLevel(settings.Trace);
			var versionInfo = settings.ValidateVersionInfo();
			if (versionInfo is null) return settings.GetReturnCode(1);

			var newTagNameResult = versionInfo.ResolveSchema(versionInfo.CurrentBranchConfig.NewTagSchema);

			if (!newTagNameResult)
			{
				AnsiConsole.MarkupLine("No tag schema configured. Aborting!");
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
}