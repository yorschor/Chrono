using System.Diagnostics;
using Chrono.Core.Test;
using LibGit2Sharp;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Chrono.CLI.Test;

public class CliTestHelper : CoreTestHelper
{
    private readonly CommandAppTester _fixture;

    #region Setup

    public CliTestHelper()
    {
        _fixture = new CommandAppTester();
        _fixture.Configure(Chrono.GetAppConfigurator());
    }
    
    #endregion

    public void RunAndAssert(string[] command, string expectedOutput)
    {
        Directory.SetCurrentDirectory(TempDirectory);
        var console = new TestConsole();
        AnsiConsole.Console = console;
        _fixture.Run(command);
        if (!string.IsNullOrEmpty(expectedOutput))
        {
            Assert.Equivalent(expectedOutput, console.Output.Trim());
        }

        Debug.WriteLine($"Match: {string.Join(" ", command)} -> <{expectedOutput}>");
    }
}