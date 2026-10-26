namespace TestExplorerDump;

public class DumpCommand(TestExplorerWindow window, ITreeFormatter formatter, TextWriter output) : ICommand
{
    public int Execute()
    {
        formatter.Write(window.Read(), output);
        return 0;
    }
}
