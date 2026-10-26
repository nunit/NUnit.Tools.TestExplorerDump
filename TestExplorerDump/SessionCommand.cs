namespace TestExplorerDump;

/// <summary>
/// Full round trip: start Visual Studio and rebuild, dump the discovered tree, run tests, dump again, quit.
/// Visual Studio is quit even when a step fails, since this command started it.
/// </summary>
public class SessionCommand(VisualStudioLauncher launcher, TestExplorerLocator locator, DumpWriter dumpWriter, ConsoleWriters writers, string path, string? filter) : ICommand
{
    public int Execute()
    {
        var visualStudio = launcher.Start(path, writers.Progress);
        try
        {
            var window = locator.ForProcess(visualStudio.ProcessId).OpenWindow(expandAll: true);
            window.WaitForTests();
            writers.Progress.WriteLine("Reading discovered tree");
            var discovered = new TreeSnapshot("discovered", window.Read());

            writers.Progress.WriteLine(filter is null ? "Running all tests" : $"Running tests matching {filter}");
            new RunCommand(visualStudio, window, filter).Execute();
            window.WaitForTests();
            writers.Progress.WriteLine("Reading tree after run");
            dumpWriter.Write(visualStudio.SolutionPath, [discovered, new TreeSnapshot("after run", window.Read())]);
            return 0;
        }
        finally
        {
            writers.Progress.WriteLine("Quitting Visual Studio");
            visualStudio.Quit();
        }
    }
}
