namespace TestExplorerDump.Tests;

[TestFixture]
public class TextFormatterTests
{
    [Test]
    public void Write_IndentsTwoSpacesPerLevel()
    {
        var tree = new TestNode("Project", "P", "Passed", 2,
        [
            new TestNode("Class", "C", "Passed", 2,
            [
                new TestNode(null, "Test1", "Passed", null, []),
                new TestNode(null, "Test2", "Not Run", null, []),
            ]),
        ]);
        var output = new StringWriter { NewLine = "\n" };

        new TextFormatter().Write([tree], output);

        Assert.That(output.ToString(), Is.EqualTo(
            """
            Project: P [Passed] (2)
              Class: C [Passed] (2)
                Test1 [Passed]
                Test2 [Not Run]

            """.ReplaceLineEndings("\n")));
    }

    [Test]
    public void Write_Snapshots_HeaderPerSnapshotAndBlankLineBetween()
    {
        var output = new StringWriter { NewLine = "\n" };

        new TextFormatter().Write(
        [
            new TreeSnapshot("discovered", [new TestNode(null, "T", "Not Run", null, [])]),
            new TreeSnapshot("after run", [new TestNode(null, "T", "Passed", null, [])]),
        ], output);

        Assert.That(output.ToString(), Is.EqualTo("# discovered\nT [Not Run]\n\n# after run\nT [Passed]\n"));
    }
}
