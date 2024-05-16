using Spectre.Console.Cli;

namespace Chrono.Cli.Commands;

#region BaseSettings

public class VersionSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class GetVersionCommand : Command<GetVersionCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        throw new NotImplementedException();
    }

    public sealed class Settings : VersionSettings
    {
    }
}

#endregion