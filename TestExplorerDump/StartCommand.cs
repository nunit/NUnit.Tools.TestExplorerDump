namespace TestExplorerDump;

/// <summary>Starts a new Visual Studio and leaves it running, so other commands can use it by pid.</summary>
public class StartCommand(VisualStudioLauncher launcher, string path, ConsoleWriters writers) : ICommand
{
    public int Execute()
    {
        var visualStudio = launcher.Start(path, writers.Progress);
        writers.Out.WriteLine($"Started Visual Studio, pid {visualStudio.ProcessId}. Use --pid {visualStudio.ProcessId} with the other commands.");
        return 0;
    }
}
