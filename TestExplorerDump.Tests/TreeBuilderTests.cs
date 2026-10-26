namespace TestExplorerDump.Tests;

[TestFixture]
public class TreeBuilderTests
{
    [Test]
    public void Build_ExpandedGroups_NestRowsByCount()
    {
        var roots = TreeBuilder.Build(
        [
            Group("Project", "P", 3),
            Group("Class", "A", 2),
            Leaf("A1"),
            Group("Test Group", "G", 1),
            Leaf("G1"),
            Group("Class", "B", 1),
            Leaf("B1"),
        ]);

        Assert.That(Render(roots), Is.EqualTo("P(A(A1,G(G1)),B(B1))"));
    }

    [Test]
    public void Build_CollapsedGroup_HasNoChildrenAndStillCountsItsLeaves()
    {
        var roots = TreeBuilder.Build(
        [
            Group("Project", "P", 3),
            Group("Class", "A", 2, expanded: false),
            Group("Class", "B", 1),
            Leaf("B1"),
        ]);

        Assert.That(Render(roots), Is.EqualTo("P(A,B(B1))"));
    }

    [Test]
    public void Build_SeveralRoots_ReturnsEach()
    {
        var roots = TreeBuilder.Build([Group("Project", "P1", 1), Leaf("T1"), Group("Project", "P2", 1), Leaf("T2")]);

        Assert.That(Render(roots), Is.EqualTo("P1(T1),P2(T2)"));
    }

    [Test]
    public void Build_RowsEndBeforeCountIsReached_StopsAtEnd()
    {
        var roots = TreeBuilder.Build([Group("Project", "P", 5), Leaf("T1")]);

        Assert.That(Render(roots), Is.EqualTo("P(T1)"));
    }

    private static TreeItemCells Group(string kind, string name, int count, bool expanded = true) =>
        new($"{kind}: {name} Passed ({count})", name, "Passed", $"({count})", expanded);

    private static TreeItemCells Leaf(string name) => new($"{name} Passed", name, "Passed", null, false);

    private static string Render(IEnumerable<TestNode> nodes) =>
        string.Join(",", nodes.Select(n => n.Children.Count == 0 ? n.Name : $"{n.Name}({Render(n.Children)})"));
}
