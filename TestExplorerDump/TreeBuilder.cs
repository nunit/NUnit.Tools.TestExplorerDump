namespace TestExplorerDump;

/// <summary>
/// Rebuilds the tree from rows in display order. UI Automation parent links are unreliable for
/// Test Explorer rows, but every group shows its test count, so an expanded group owns the rows
/// that follow it until that many leaves have been seen.
/// </summary>
public static class TreeBuilder
{
    public static IReadOnlyList<TestNode> Build(IReadOnlyList<TreeItemCells> rows)
    {
        var position = 0;
        var roots = new List<TestNode>();
        while (position < rows.Count)
            roots.Add(Next(rows, ref position).Node);
        return roots;
    }

    private static (TestNode Node, int Leaves) Next(IReadOnlyList<TreeItemCells> rows, ref int position)
    {
        var row = rows[position++];
        if (!row.IsGroup)
            return (row.ToNode([]), 1);

        var count = row.Count!.Value;
        var children = new List<TestNode>();
        for (var leaves = 0; row.Expanded && leaves < count && position < rows.Count;)
        {
            var (child, childLeaves) = Next(rows, ref position);
            children.Add(child);
            leaves += childLeaves;
        }

        return (row.ToNode(children), count);
    }
}
