using Chrono.Core;
using Chrono.Core.Helpers;
using Chrono.InitHelpers;
using Spectre.Console;
using Spectre.Console.Cli;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class InitSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class InitCommand : Command<InitCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Trace)
        {
            NLogHelper.EnableShortConsoleTarget(true);
        }

        var projectDirectory = Directory.GetCurrentDirectory();
        var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");
        var directoryBuildPropsPath = Path.Combine(projectDirectory, "Directory.Build.props");

        if (!File.Exists(packagesConfigPath))
        {
            File.WriteAllText(directoryBuildPropsPath, BuildProps.Get());
            Console.WriteLine($"Created {directoryBuildPropsPath} with Chrono.DotnetTasks package reference.");
        }
        else
        {
            Console.WriteLine($"The {packagesConfigPath} file already exists. No changes made.");
        }
       
        {
            NLogHelper.EnableShortConsoleTarget();
            return 0;
        }
    }

    public sealed class Settings : InitSettings {
    }
}

#endregion