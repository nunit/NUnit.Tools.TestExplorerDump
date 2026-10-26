namespace TestExplorerDump;

public class BuildCommand(DteClient visualStudio) : ICommand
{
    public int Execute()
    {
        visualStudio.Build();
        return 0;
    }
}
