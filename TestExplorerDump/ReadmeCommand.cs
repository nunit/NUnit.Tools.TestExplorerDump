using System.Diagnostics;

namespace TestExplorerDump;

/// <summary>Opens the README on GitHub, where it is rendered, in Microsoft Edge.</summary>
public class ReadmeCommand(TextWriter output) : ICommand
{
    private const string ReadmeUrl = "https://github.com/nunit/NUnit.Tools.TestExplorerDump#readme";

    public int Execute()
    {
        // UseShellExecute resolves msedge through the App Paths registry key, so no install path is needed.
        using var _ = Process.Start(new ProcessStartInfo("msedge", ReadmeUrl) { UseShellExecute = true });
        output.WriteLine($"Opened {ReadmeUrl}");
        return 0;
    }
}
