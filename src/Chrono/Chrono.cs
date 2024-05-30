using Chrono.Commands;
using NLog;
using Spectre.Console.Cli;

namespace Chrono;

public static class Chrono
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("chrono"); 
            
            config.AddCommand<InitCommand>("init")
                .WithDescription("")
                .WithExample("init");
            
            config.AddCommand<GetVersionCommand>("get")
                .WithDescription("")
                .WithExample("get");
            config.AddCommand<SetVersionCommand>("set")
                .WithDescription("")
                .WithExample("set");

            config.AddCommand<GetInfoCommand>("info");

        });
        return app.Run(args);
    }
}

public class BaseCommandSettings : CommandSettings
{
    public readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [CommandOption("-d|--debug")] public bool Debug { get; init; } = false;
    [CommandOption("-t|--trace")] public bool Trace { get; init; } = false;
}