namespace TestExplorerDump;

public interface ICommand
{
    /// <returns>Process exit code.</returns>
    int Execute();
}
