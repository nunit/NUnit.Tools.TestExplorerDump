namespace TestExplorerDump.Tests;

[TestFixture]
public class DumpWriterTests
{
    private string folder = null!;

    [SetUp]
    public void SetUp() => folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(folder, recursive: true);

    [Test]
    public void Write_Session_OneFilePerSnapshotInTeDumpFolderNextToSolution()
    {
        var output = new StringWriter();
        var writer = new DumpWriter(new TextFormatter(), new ConsoleWriters(output, new StringWriter()), outFolder: null);

        writer.Write(Path.Combine(folder, "Issue1408.slnx"),
        [
            new TreeSnapshot("discovered", [new TestNode(null, "T", "Not Run", null, [])]),
            new TreeSnapshot("after run", [new TestNode(null, "T", "Passed", null, [])]),
        ]);

        var files = Directory.GetFiles(Path.Combine(folder, "te-dump")).Select(Path.GetFileName).Order().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(files, Has.Count.EqualTo(2));
            Assert.That(files[0], Does.Match(@"^Issue1408-\d{8}-\d{6}-after-run\.txt$"));
            Assert.That(files[1], Does.Match(@"^Issue1408-\d{8}-\d{6}-discovered\.txt$"));
            Assert.That(File.ReadAllText(Path.Combine(folder, "te-dump", files[1])).Trim(), Is.EqualTo("T [Not Run]"));
            Assert.That(output.ToString(), Does.Contain("# after run"));
        }
    }

    [Test]
    public void Write_OutFolder_IsUsedInsteadOfSolutionFolder()
    {
        var outFolder = Path.Combine(folder, "dumps");
        var writer = new DumpWriter(new JsonFormatter(), new ConsoleWriters(new StringWriter(), new StringWriter()), outFolder);

        writer.Write(@"C:\elsewhere\Issue489.slnx", [new TreeSnapshot("dump", [])]);

        Assert.That(Directory.GetFiles(outFolder).Select(Path.GetFileName), Has.One.Match(@"^Issue489-\d{8}-\d{6}-dump\.json$"));
    }
}
