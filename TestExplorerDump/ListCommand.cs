namespace TestExplorerDump;

/// <summary>Prints the numbered instance list and saves it, so "te-dump 2" can pick instance 2.</summary>
public class ListCommand(TestExplorerLocator locator, InstanceListFile instanceList, TextWriter output) : ICommand
{
    public int Execute()
    {
        var instances = locator.FindAll().Select(p => p.Instance).ToList();
        instanceList.Save(instances);
        output.WriteLine(instances.Count == 0 ? "No Visual Studio instance has Test Explorer open." : VsInstance.Numbered(instances));
        return 0;
    }
}
