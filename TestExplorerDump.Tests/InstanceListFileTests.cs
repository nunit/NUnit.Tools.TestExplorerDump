namespace TestExplorerDump.Tests;

[TestFixture]
public class InstanceListFileTests
{
    private string path = null!;

    [SetUp]
    public void SetUp() => path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [TearDown]
    public void TearDown() => File.Delete(path);

    [TestCase(1, ExpectedResult = 7700)]
    [TestCase(3, ExpectedResult = 73160)]
    public int ProcessIdOf_SavedList_ReturnsPidByNumber(int number)
    {
        var list = new InstanceListFile(path);
        list.Save([new VsInstance(7700, "A"), new VsInstance(16444, "B"), new VsInstance(73160, "C")]);

        return list.ProcessIdOf(number);
    }

    [Test]
    public void ProcessIdOf_NumberOutsideList_Throws()
    {
        var list = new InstanceListFile(path);
        list.Save([new VsInstance(7700, "A")]);

        Assert.That(() => list.ProcessIdOf(2), Throws.InvalidOperationException.With.Message.Contains("No instance 2"));
    }

    [Test]
    public void ProcessIdOf_NoFile_Throws()
    {
        Assert.That(() => new InstanceListFile(path).ProcessIdOf(1), Throws.InvalidOperationException.With.Message.Contains("No saved instance list"));
    }
}
