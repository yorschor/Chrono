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
            
        });
        return app.Run(args);
    }
}

public class BaseCommandSettings : CommandSettings
{
    public readonly ILogger Logger = LogManager.GetCurrentClassLogger();
}