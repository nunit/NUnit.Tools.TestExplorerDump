namespace TestExplorerDump;

public class BuildCommand(DteClient visualStudio) : ICommand
{
    public int Execute()
    {
        visualStudio.Rebuild();
        return 0;
    }
}
