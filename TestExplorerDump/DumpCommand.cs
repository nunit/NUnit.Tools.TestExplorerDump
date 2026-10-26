namespace TestExplorerDump;

public class DumpCommand(TestExplorerWindow window, DumpWriter dumpWriter, string solutionPath) : ICommand
{
    public int Execute()
    {
        dumpWriter.Write(solutionPath, [new TreeSnapshot("dump", window.Read())]);
        return 0;
    }
}
