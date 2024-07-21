using Chrono.Commands;
using NLog;
using Spectre.Console.Cli;

namespace Chrono;

public static class Chrono
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(GetAppConfigurator());
        return app.Run(args);
    }

    public static Action<IConfigurator> GetAppConfigurator() => config =>
    {
        config.SetApplicationName("chrono");

        config.AddCommand<InitCommand>("init")
            .WithDescription("Initializes the current directory with the required files for Chrono to work")
            .WithExample("init");

        config.AddCommand<GetVersionCommand>("get")
            .WithDescription("Gets the current version of the project")
            .WithExample("get");

        config.AddCommand<SetVersionCommand>("set")
            .WithDescription("Sets the current version of the project")
            .WithExample("set");

        config.AddCommand<BumpVersionCommand>("bump")
            .WithDescription("Increments the current version of the project")
            .WithExample("bump");

        config.AddBranch("create", createConfig =>
        {
            createConfig.AddCommand<CreateReleaseBranchCommand>("release")
                .WithDescription("Creates a new release branch");
            createConfig.AddCommand<CreateTagCommand>("tag")
                .WithDescription("Creates a new tag");
            createConfig.AddCommand<CreateBranchCommand>("release")
                .WithDescription("Creates a new branch");
        });

        config.AddCommand<GetInfoCommand>("info")
            .WithDescription("Gets information about Chrono");
    };
}

public class BaseCommandSettings : CommandSettings
{
    public readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [CommandOption("-d|--debug")] public bool Debug { get; init; } = false;
    [CommandOption("-t|--trace")] public bool Trace { get; init; } = false;
}