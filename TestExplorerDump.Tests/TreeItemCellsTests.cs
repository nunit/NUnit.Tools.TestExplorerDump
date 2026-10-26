namespace TestExplorerDump.Tests;

[TestFixture]
public class TreeItemCellsTests
{
    [TestCase("Project: Issue489 Passed (9)", "Issue489", "(9)", "Project", 9)]
    [TestCase("Namespace: A.B Not Run (12)", "A.B", "(12)", "Namespace", 12)]
    [TestCase("Test Group: Ns.Class.Method Passed (2)", "Method", "(2)", "Test Group", 2)]
    [TestCase("SimpleTest Passed", "SimpleTest", null, null, null)]
    [TestCase("Weird: name Passed", "Weird: name", null, null, null)]
    public void ToNode_ParsesKindAndCount(string automationName, string label, string? countText, string? kind, int? count)
    {
        var node = new TreeItemCells(automationName, label, "Passed", countText, true).ToNode([]);

        Assert.That(node, Is.EqualTo(new TestNode(kind, label, "Passed", count, node.Children)));
    }

    [TestCase("(3)", ExpectedResult = true)]
    [TestCase(null, ExpectedResult = false)]
    [TestCase("5 ms", ExpectedResult = false)]
    public bool IsGroup_IsTrueOnlyWithCount(string? countText) =>
        new TreeItemCells("Name", "Name", null, countText, false).IsGroup;
}
