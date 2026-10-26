namespace TestExplorerDump;

public record VsInstance(int ProcessId, string Title)
{
    /// <summary>One line per instance, numbered from 1, as "te-dump &lt;number&gt;" expects.</summary>
    public static string Numbered(IReadOnlyList<VsInstance> instances) =>
        string.Join(Environment.NewLine, instances.Select((v, i) => $"  {i + 1}  {v.ProcessId,6}  {v.Title}"));
}
