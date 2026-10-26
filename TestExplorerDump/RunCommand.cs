namespace TestExplorerDump;

/// <summary>Runs all tests, or only those Test Explorer shows for a search text.</summary>
public class RunCommand(DteClient visualStudio, TestExplorerWindow window, string? filter) : ICommand
{
    public int Execute()
    {
        if (filter is null)
        {
            visualStudio.RunTestsAndWait(inViewOnly: false);
            return 0;
        }

        window.Search(filter);
        try
        {
            visualStudio.RunTestsAndWait(inViewOnly: true);
        }
        finally
        {
            window.Search("");
        }

        return 0;
    }
}
