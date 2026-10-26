namespace TestExplorerDump;

/// <summary>
/// Writes trees to stdout and to one file per snapshot, by default in a "te-dump" folder next to the
/// solution: &lt;Solution&gt;-&lt;yyyyMMdd-HHmmss&gt;-&lt;snapshot&gt;.txt (or .json). The timestamp keeps the
/// history, so dumps from before and after an adapter change can be diffed.
/// </summary>
public class DumpWriter(ITreeFormatter formatter, ConsoleWriters writers, string? outFolder)
{
    public void Write(string solutionPath, IReadOnlyList<TreeSnapshot> snapshots)
    {
        if (snapshots.Count == 1)
            formatter.Write(snapshots[0].Roots, writers.Out);
        else
            formatter.Write(snapshots, writers.Out);

        var folder = outFolder ?? Path.Combine(Path.GetDirectoryName(solutionPath)!, "te-dump");
        Directory.CreateDirectory(folder);
        var prefix = $"{Path.GetFileNameWithoutExtension(solutionPath)}-{DateTime.Now:yyyyMMdd-HHmmss}";
        foreach (var snapshot in snapshots)
        {
            var path = Path.Combine(folder, $"{prefix}-{snapshot.Name.Replace(' ', '-')}.{formatter.FileExtension}");
            using (var file = new StreamWriter(path))
                formatter.Write(snapshot.Roots, file);
            writers.Progress.WriteLine($"Wrote {path}");
        }
    }
}
