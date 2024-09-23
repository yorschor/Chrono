using System.Reflection;
using Chrono.Core;
using Chrono.Core.Helpers;
using Chrono.Helpers;
using Huxy;
using LibGit2Sharp;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Core;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

namespace Chrono.Commands;

#region BaseSettings

public class InfoSettings : BaseCommandSettings
{
}

#endregion

#region Commands

public class GetInfoCommand : Command<GetInfoCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("TopSpace"),
                new Layout("Figlet").Ratio(2),
                new Layout("Version"),
                new Layout("Description"),
                new Layout("Footer")
                    .SplitColumns(
                        new Layout("Left"),
                        new Layout("Right")));
        var rootPanel = new Panel(layout)
        {
            Border = BoxBorder.None
        };
        layout["TopSpace"].Update(new Rule());
        layout["Figlet"].Update(new FigletText(FFont.Get, "Chrono").Centered().Justify(Justify.Center).Color(Color.Green));
        layout["Version"].Update(Align.Center(new Markup($"[bold]Version:[/] {Assembly.GetEntryAssembly().GetName().Version}")));
        layout["Description"]
            .Update(Align.Center(new Text("Easy Git versioning for the rest of us \n your project | your version | your rules")));
        layout["Left"].Update(Align.Left(new Markup("[blue3_1]https://github.com/yorschor/Chrono[/]"), VerticalAlignment.Bottom));
        layout["Right"].Update(Align.Right(new Markup("Made with ❤️ by [deeppink3_1]Ekkehard C. Damisch[/] in [red]Austria[/]"),
            VerticalAlignment.Bottom));

        AnsiConsole.Write(rootPanel);
        return 0;
    }

    public sealed class Settings : InfoSettings
    {
    }
}

#endregion