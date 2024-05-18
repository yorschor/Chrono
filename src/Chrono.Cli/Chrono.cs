using Chrono.Cli.Commands;
using NLog;
using Spectre.Console.Cli;

namespace Chrono.Cli;

public static class Chrono
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("chrono"); 
            
            config.AddCommand<GetVersionCommand>("get")
                .WithDescription("")
                .WithExample("get");
            
        });
        return app.Run(args);
    }
}

public class BaseCommandSettings : CommandSettings
{
    public readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [CommandOption("-d|--debug")] public bool Debug { get; init; } = false;
}