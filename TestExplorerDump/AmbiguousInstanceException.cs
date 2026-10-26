namespace TestExplorerDump;

/// <summary>More than one Visual Studio instance matches. The message lists them numbered from 1.</summary>
public class AmbiguousInstanceException(IReadOnlyList<VsInstance> instances)
    : InvalidOperationException(
        "More than one Visual Studio instance matches. Pick one with te-dump <number>, --pid or --title:"
        + Environment.NewLine + VsInstance.Numbered(instances))
{
    public IReadOnlyList<VsInstance> Instances { get; } = instances;
}
