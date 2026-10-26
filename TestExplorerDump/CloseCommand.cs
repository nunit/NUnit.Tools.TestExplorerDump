namespace TestExplorerDump;

public class CloseCommand(DteClient visualStudio) : ICommand
{
    public int Execute()
    {
        visualStudio.Quit();
        return 0;
    }
}
