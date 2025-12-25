using Chrono.Core;
using Chrono.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands.Core;

#region Commands

public class BumpVersionCommand : Command<BumpVersionCommand.Settings>
{
	public sealed class Settings : BaseCommandSettings
	{
		[CommandArgument(0, "<Version Component>")]
		public VersionComponent VersionComponent { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		NLogHelper.SetLogLevel(settings.Trace);

		var versionInfo = settings.ValidateVersionInfo();
		if (versionInfo is null) return settings.GetReturnCode(1);
		var res = versionInfo.BumpVersion(settings.VersionComponent);
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
}

#endregion