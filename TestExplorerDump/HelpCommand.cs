namespace TestExplorerDump;

public class HelpCommand(TextWriter output) : ICommand
{
    public int Execute()
    {
        output.WriteLine(Options.Usage);
        return 0;
    }
}
