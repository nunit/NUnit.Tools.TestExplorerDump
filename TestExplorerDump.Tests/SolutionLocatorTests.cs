namespace TestExplorerDump.Tests;

[TestFixture]
public class SolutionLocatorTests
{
    private string folder = null!;

    [SetUp]
    public void SetUp() => folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(folder, recursive: true);

    [TestCase("A.slnx,A.csproj", ExpectedResult = "A.slnx")]
    [TestCase("A.sln,A.csproj", ExpectedResult = "A.sln")]
    [TestCase("A.csproj,Tests.cs", ExpectedResult = "A.csproj")]
    public string Find_Folder_PrefersSolutionOverProject(string files)
    {
        foreach (var file in files.Split(','))
            File.WriteAllText(Path.Combine(folder, file), "");

        return Path.GetFileName(new SolutionLocator().Find(folder));
    }

    [Test]
    public void Find_File_ReturnsIt()
    {
        var project = Path.Combine(folder, "A.csproj");
        File.WriteAllText(project, "");

        Assert.That(new SolutionLocator().Find(project), Is.EqualTo(project));
    }

    [TestCase("", "No .slnx")]
    [TestCase("A.slnx,B.sln", "More than one candidate")]
    public void Find_NoneOrSeveral_Throws(string files, string message)
    {
        foreach (var file in files.Split(',', StringSplitOptions.RemoveEmptyEntries))
            File.WriteAllText(Path.Combine(folder, file), "");

        Assert.That(() => new SolutionLocator().Find(folder), Throws.InvalidOperationException.With.Message.Contains(message));
    }
}
