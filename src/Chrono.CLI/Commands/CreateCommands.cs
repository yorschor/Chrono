using Chrono.Core;
using Chrono.Core.Helpers;
using Huxy;
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

public class CreateReleaseBranchCommand : Command<CreateReleaseBranchCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        // Create new branch acording to schema 
        // Increment Version on existing branch arcording to schema 
        //  
        var infoGetResult = VersionInfo.Get();
        if (infoGetResult is IErrorResult)
        {
            settings.Logger.Error(infoGetResult.Message);
            return 0;
        }

        var parseFullVersionResult = infoGetResult.Data.ParseVersion();
        if (!parseFullVersionResult.Success) return 0;

        var currentBranchConfig=  infoGetResult.Data.GetConfigForCurrentBranch().Data;
        
        // Create new Branch. 
        // Increment Version on existing branch arcording to schema
        
        
        // infoGetResult.Data.
        return 1;
    }

    public sealed class Settings : GitSettings
    {
    }
}

public class CreateTagCommand : Command<CreateTagCommand.Settings>
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