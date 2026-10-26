namespace TestExplorerDump;

public interface ITreeFormatter
{
    void Write(IReadOnlyList<TestNode> roots, TextWriter output);

    void Write(IReadOnlyList<TreeSnapshot> snapshots, TextWriter output);
}
