using Chrono.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chrono.Commands.Core;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global
public class SetVersionCommand : Command<SetVersionCommand.Settings>
{
	public sealed class Settings : BaseCommandSettings
	{
		[CommandArgument(0, "<VERSION>")] public string NewVersion { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
}