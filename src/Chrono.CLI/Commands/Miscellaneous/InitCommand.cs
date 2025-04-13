using Chrono.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands.Miscellaneous;

#region Commands

public class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : BaseCommandSettings;

    public override int Execute(CommandContext context, Settings settings)
    {
        var projectDirectory = Directory.GetCurrentDirectory();

        // === STEP 1. Preflight Check ===
        // Check for presence of key files, printing statuses accordingly:
        // - version.yml : if exists -> red; if not -> green.
        // - Directory.Build.props : if exists -> red; if not -> green.
        // - packages.config : search recursively at least 3 directory layers deep;
        //                    if exists -> warn (yellow).
        var versionFileExists = File.Exists(Path.Combine(projectDirectory, "version.yml"));
        var buildPropsExists = File.Exists(Path.Combine(projectDirectory, "Directory.Build.props"));

        // A helper method to search for packages.config files 3 levels deep at most.
        var packagesConfigPaths = Directory.EnumerateFiles(projectDirectory, "packages.config", SearchOption.AllDirectories);
        var packagesConfigExists = packagesConfigPaths.Any();

        // Print status for version.yml
        AnsiConsole.Write(new Rule($"[bold]Chrono Init[/]"));
        
        if (versionFileExists)
            AnsiConsole.MarkupLine("version.yml - [red]exists[/]");
        else
            AnsiConsole.MarkupLine("version.yml - [green]Not found[/]");

        // Print status for Directory.Build.props
        if (buildPropsExists)
            AnsiConsole.MarkupLine("Directory.Build.props - [red]exists.[/]");
        else
            AnsiConsole.MarkupLine("Directory.Build.props - [green]not found[/]");

        // Print status for packages.config (if found, list each and show warning)
        if (packagesConfigExists)
        {
            foreach (var path in packagesConfigPaths)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: packages.config found at '{path}'. This might interfere with Chrono![/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[green]packages.config not found in subdirectories (up to 3 levels deep).[/]");
        }

        // If any red status (e.g. version.yml already exists), ask if user wishes to proceed.
        var proceedAfterPreflight = true;
        if (versionFileExists || buildPropsExists)
        {
            proceedAfterPreflight = AnsiConsole.Confirm("One or more files already exist. Do you wish to proceed (files might be overwritten)?", false);
        }

        if (!proceedAfterPreflight)
        {
            AnsiConsole.MarkupLine("[red]Aborting[/]");
            return -1;
        }

        // === STEP 2. Get Initial Version ===
        var initialVersion = AnsiConsole.Prompt(new TextPrompt<string>("What is your initial version for the project?").DefaultValue("0.0.1.0"));

        // === STEP 3. Choose a Version Schema Variant ===
        var variantString = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which variant of the version schema do you want to use?")
                .AddChoices("(local) Minimal", "(local) Default", "(local) Expanded", "(remote inherited) Default", "(remote inherited) Expanded"));

        var variant = variantString switch
        {
            "(local) Minimal" => VersionFileVariants.LocalMinimal,
            "(local) Default" => VersionFileVariants.LocalDefault,
            "(local) Expanded" => VersionFileVariants.LocalExpanded,
            "(remote inherited) Default" => VersionFileVariants.InheritedDefault,
            "(remote inherited) Expanded" => VersionFileVariants.InheritedExpanded,
            _ => VersionFileVariants.LocalMinimal,
        };

        // === STEP 4. Choose if Directory.Build.props Should be Created ===
        var includeBuildProps = AnsiConsole.Confirm("Do you need a Directory.Build.props file to reference Chrono in multiple projects?", !buildPropsExists);

        // === STEP 5. Preview Files to be Written ===

        var versionContent = InitTemplates.GetVersionFile(variant, initialVersion);

        AnsiConsole.Write(new Rule($"[bold]version.yml[/]"));
        Console.WriteLine(versionContent);
        AnsiConsole.WriteLine();
        if (includeBuildProps)
        {
            var propsContent = InitTemplates.GetBuildProps(settings.AppVersion);
            AnsiConsole.Write(new Rule($"[bold]Directory.Build.props[/]"));
            Console.WriteLine(propsContent);
            AnsiConsole.WriteLine();
        }

        // === STEP 6. Confirm & Write Files to Disk ===
        var writeFiles = AnsiConsole.Confirm("Proceed with writing these files to disk? ([red]Warning:[/] [bold]existing[/] files may be [bold]overwritten![/])", false);
        if (writeFiles)
        {
            File.WriteAllText(Path.Combine(projectDirectory, "version.yml"),
                InitTemplates.GetVersionFile(variant, initialVersion));

            if (includeBuildProps)
            {
                File.WriteAllText(Path.Combine(projectDirectory, "Directory.Build.props"),
                    InitTemplates.GetBuildProps(settings.AppVersion));
            }

            AnsiConsole.MarkupLine("[green]Files have been written successfully.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Files not written.[/]");
        }

        return 0;
    }
}

#endregion