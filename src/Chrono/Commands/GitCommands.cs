using Chrono.Core;
using Chrono.Core.Helpers;
using Nuke.Common.Utilities.Collections;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class GitSettings : BaseCommandSettings
{
    
}

#endregion

#region Commands

public class ReleaseCommands : Command<ReleaseCommands.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        // Create new branch acording to schema 
        // Increment Version on existing branch arcording to schema 
        //  
        
        return 1;
    }

    public sealed class Settings : GitSettings
    {
    }
}

public class TagCommand : Command<TagCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        return 1;
    }

    public sealed class Settings : GitSettings
    {
        [CommandArgument(0, "<VERSION>")] public string NewVersion { get; set; }
    }
}

#endregion