namespace TestExplorerDump;

public interface ITreeFormatter
{
    /// <summary>Extension for dump files, without the dot.</summary>
    string FileExtension { get; }

    void Write(IReadOnlyList<TestNode> roots, TextWriter output);

    void Write(IReadOnlyList<TreeSnapshot> snapshots, TextWriter output);
}
