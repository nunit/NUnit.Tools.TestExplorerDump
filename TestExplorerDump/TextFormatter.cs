namespace TestExplorerDump;

/// <summary>Indented text, two spaces per level. Snapshots get a "# name" header line.</summary>
public class TextFormatter : ITreeFormatter
{
    public string FileExtension => "txt";

    public void Write(IReadOnlyList<TestNode> roots, TextWriter output)
    {
        foreach (var root in roots)
            root.WriteTo(output, 0);
    }

    public void Write(IReadOnlyList<TreeSnapshot> snapshots, TextWriter output)
    {
        for (var i = 0; i < snapshots.Count; i++)
        {
            if (i > 0)
                output.WriteLine();
            output.WriteLine($"# {snapshots[i].Name}");
            Write(snapshots[i].Roots, output);
        }
    }
}
