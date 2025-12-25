using Chrono.Core.Helpers;
using Chrono.Helpers;
using LibGit2Sharp;
using Spectre.Console.Cli;

namespace Chrono.Commands.Git;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global
public class CreateReleaseBranchCommand : Command<CreateReleaseBranchCommand.Settings>
{
	public sealed class Settings : BaseCommandSettings
	{
		[CommandArgument(0, "[BranchKey]")] public string BranchKey { get; set; } = "Default_Release_Config";

		[CommandOption("-c|--commit")] public bool Commit { get; init; } = false;

		[CommandArgument(1, "[Commit Message {oldVersion} {newVersion}]")]
		public string CommitMessage { get; init; } = "Chrono: Set version {oldVersion} => {newVersion}";
	}

	public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		NLogHelper.SetLogLevel(settings.Trace);
		var versionInfo = settings.ValidateVersionInfo();
		if (versionInfo is null) return settings.GetReturnCode(1);

		// 1 Create new branch according to schema without checking out
		var newBranchNameResult = versionInfo.GetNewBranchNameFromKey(settings.BranchKey);

		if (!newBranchNameResult)
		{
			newBranchNameResult.PrintFailures();
			return 1;
		}

		var repo = settings.GetRepo().Data;
		settings.MarkupAndTrace($"Creating new branch {newBranchNameResult.Data}");
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

			var commit = repo.Commit(
				settings.CommitMessage.Replace("{oldVersion}", oldversion.Data)
					.Replace("{newVersion}", newVersion.Data), author,
				author);
		}

		NLogHelper.SetLogLevel(false);
		return 0;
	}
}