using Chrono.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class InitSettings : BaseCommandSettings;

#endregion

#region Commands

public class InitCommand : Command<InitCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var files = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Which files to set up?")
                .PageSize(10)
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a file, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices("Directory.Build.props", "version.yml"));

        var variant = VersionFileVariants.LocalMinimal;
        var initialVersion ="0.0.1";
        if (files.Contains("version.yml"))
        {
            var variantString = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What kind of version.yml do you need?")
                    .PageSize(10)
                    .AddChoices("(local) Minimal", "(local) default", "(inherited) minimal", "(inherited) default)"));
            variant = variantString switch
            {
                "(local) Minimal" => VersionFileVariants.LocalMinimal,
                "(local) default" => VersionFileVariants.LocalDefault,
                "(inherited) minimal" => VersionFileVariants.InheritedMinimal,
                "(inherited) default)" => VersionFileVariants.InheritedDefault,
                _ => VersionFileVariants.LocalMinimal
            };
            initialVersion = AnsiConsole.Prompt(new TextPrompt<string>("What´s your initial version (default 0.0.1)?").DefaultValue("0.0.1"));
        }

        if (settings.Print)
        {
            AnsiConsole.MarkupLine("[bold]--- Directory.Build.props ---[/]");
            AnsiConsole.Write(InitTemplates.GetBuildProps(settings.AppVersion));
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[bold]-----------------------------------------[/]");
            AnsiConsole.MarkupLine("[bold]--- version.yml ---[/]");
            AnsiConsole.Write(InitTemplates.GetVersionFile(variant, initialVersion));

            return 0;
        }
        
        var projectDirectory = Directory.GetCurrentDirectory();
        var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");
        var directoryBuildPropsPath = Path.Combine(projectDirectory, "Directory.Build.props");
        var versionFilePath = Path.Combine(projectDirectory, "version.yml");
        
        var packagesExist = File.Exists(packagesConfigPath);
        var directoryBuildPropsExist = File.Exists(directoryBuildPropsPath);
        var versionFileExist = File.Exists(versionFilePath);

        var fileWritePrompt = new MultiSelectionPrompt<string>()
            .Title("Which files to set up?")
            .PageSize(10)
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a file, " +
                "[green]<enter>[/] to accept)[/]")
            .AddChoices("Directory.Build.props", "version.yml");
        if (packagesExist)
        {
            AnsiConsole.MarkupLine("[red]packages.config found! This will interfere with Chrono![/] Proceed with care.");
            if (!directoryBuildPropsExist) {fileWritePrompt.Select("Directory.Build.props");}
        }
        if (!directoryBuildPropsExist) {fileWritePrompt.Select("Directory.Build.props");}
        if (!versionFileExist) {fileWritePrompt.Select("version.yml");}
        var filesToWrite = AnsiConsole.Prompt(fileWritePrompt);
        
        if (filesToWrite.Contains("Directory.Build.props"))File.WriteAllText(directoryBuildPropsPath, InitTemplates.GetBuildProps());
        if (filesToWrite.Contains("version.yml"))File.WriteAllText(versionFilePath, InitTemplates.GetVersionFile(variant, initialVersion));
        return 0;
    }

    public sealed class Settings : InitSettings
    {
        [CommandOption("-p | --print")] public bool Print { get; set; }
    }
}

#endregion