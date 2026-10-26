namespace TestExplorerDump.Tests;

[TestFixture]
public class OptionsTests
{
    [Test]
    public void Parse_NoArguments_ReturnsDefaults()
    {
        var options = Options.Parse([]);

        Assert.That(options, Is.EqualTo(new Options(null, null, false, OutputFormat.Text)));
    }

    [Test]
    public void Parse_AllArguments_ReturnsThem()
    {
        var options = Options.Parse(["--pid", "42", "--title", "Issue489", "--expand-all", "--format", "JSON"]);

        Assert.That(options, Is.EqualTo(new Options(42, "Issue489", true, OutputFormat.Json)));
    }

    [TestCase("--pid")]
    [TestCase("--pid", "abc")]
    [TestCase("--title")]
    [TestCase("--format", "xml")]
    [TestCase("--unknown")]
    [TestCase("0")]
    [TestCase("1", "2")]
    [TestCase("2", "--pid", "7")]
    [TestCase("start", "--pid", "7")]
    [TestCase("dump", "--filter", "x")]
    [TestCase("start", "a", "b")]
    public void Parse_InvalidArguments_Throws(params string[] args)
    {
        Assert.That(() => Options.Parse(args), Throws.ArgumentException);
    }

    [Test]
    public void Parse_Number_ReturnsIt()
    {
        var options = Options.Parse(["2", "--expand-all"]);

        Assert.That(options, Is.EqualTo(new Options(null, null, true, OutputFormat.Text, 2)));
    }

    [TestCase("-s")]
    [TestCase("--short")]
    public void Parse_Short_SetsShort(string flag)
    {
        Assert.That(Options.Parse([flag]).Short, Is.True);
    }

    [TestCase("dump", ExpectedResult = Command.Dump)]
    [TestCase("list", ExpectedResult = Command.List)]
    [TestCase("build", ExpectedResult = Command.Build)]
    [TestCase("run", ExpectedResult = Command.Run)]
    [TestCase("close", ExpectedResult = Command.Close)]
    public Command Parse_CommandName_ReturnsCommand(string name) => Options.Parse([name]).Command;

    [Test]
    public void Parse_RunWithTargetAndFilter_ReturnsThem()
    {
        var options = Options.Parse(["run", "3", "--filter", "Class:\"Class1\""]);

        Assert.That(options, Is.EqualTo(new Options(null, null, false, OutputFormat.Text, 3, Command: Command.Run, Filter: "Class:\"Class1\"")));
    }

    [TestCase("start", ExpectedResult = ".")]
    [TestCase("start", "C:\\repo\\Issue489.slnx", ExpectedResult = "C:\\repo\\Issue489.slnx")]
    [TestCase("session", "..\\Issue1", "--filter", "x", ExpectedResult = "..\\Issue1")]
    public string? Parse_StartOrSession_PathDefaultsToCurrentFolder(params string[] args) => Options.Parse(args).Path;

    [Test]
    public void ResolveNumber_WithNumber_UsesPidFromList()
    {
        var path = Path.GetTempFileName();
        try
        {
            var list = new InstanceListFile(path);
            list.Save([new VsInstance(100, "A"), new VsInstance(200, "B")]);

            var options = new Options(null, null, false, OutputFormat.Text, 2).ResolveNumber(list);

            Assert.That(options, Is.EqualTo(new Options(200, null, false, OutputFormat.Text)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase(null, null, 7, "Any - Microsoft Visual Studio", ExpectedResult = true)]
    [TestCase(7, null, 7, "Any", ExpectedResult = true)]
    [TestCase(7, null, 8, "Any", ExpectedResult = false)]
    [TestCase(null, "issue489", 7, "Issue489 - Tests.cs - Microsoft Visual Studio", ExpectedResult = true)]
    [TestCase(null, "Issue1097", 7, "Issue489 - Microsoft Visual Studio", ExpectedResult = false)]
    [TestCase(7, "Issue489", 8, "Issue489", ExpectedResult = false)]
    public bool Matches_FiltersByPidAndTitle(int? processId, string? title, int actualId, string actualTitle) =>
        new Options(processId, title, false, OutputFormat.Text).Matches(actualId, actualTitle);
}
