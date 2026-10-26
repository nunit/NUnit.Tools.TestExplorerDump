namespace TestExplorerDump;

/// <summary>
/// Raw values read from one Test Explorer row. For a group, AutomationName looks like
/// "Class: Class1 Passed (9)", Label is "Class1" and CountText is "(9)". Leaves have no count.
/// </summary>
public record TreeItemCells(string AutomationName, string Label, string? Outcome, string? CountText, bool Expanded)
{
    public bool IsGroup => Count is not null;

    public int? Count => int.TryParse(CountText?.Trim('(', ')'), out var count) ? count : null;

    public TestNode ToNode(IReadOnlyList<TestNode> children) => new(Kind, Label, Outcome, Count, children);

    // The kind ("Project", "Namespace", "Class", "Test Group") only shows up as a prefix of the automation name.
    // Test Group names carry the full method name there, so split on the first ": " instead of matching the label.
    private string? Kind
    {
        get
        {
            var index = AutomationName.IndexOf(": ", StringComparison.Ordinal);
            return IsGroup && index > 0 ? AutomationName[..index] : null;
        }
    }
}
