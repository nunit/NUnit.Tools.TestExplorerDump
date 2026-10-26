namespace TestExplorerDump;

/// <summary>One node in the Test Explorer tree. Kind is null for leaf tests.</summary>
public record TestNode(string? Kind, string Name, string? Outcome, int? Count, IReadOnlyList<TestNode> Children)
{
    public string Describe()
    {
        var kind = Kind is null ? "" : $"{Kind}: ";
        var outcome = Outcome is null ? "" : $" [{Outcome}]";
        var count = Count is null ? "" : $" ({Count})";
        return kind + Name + outcome + count;
    }

    public void WriteTo(TextWriter output, int depth)
    {
        output.WriteLine(new string(' ', depth * 2) + Describe());
        foreach (var child in Children)
            child.WriteTo(output, depth + 1);
    }
}
