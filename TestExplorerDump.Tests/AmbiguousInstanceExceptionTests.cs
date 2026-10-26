namespace TestExplorerDump.Tests;

[TestFixture]
public class AmbiguousInstanceExceptionTests
{
    [Test]
    public void Message_NumbersInstancesFromOne()
    {
        var exception = new AmbiguousInstanceException([new VsInstance(7700, "A - Microsoft Visual Studio"), new VsInstance(16444, "B")]);

        Assert.That(exception.Message.ReplaceLineEndings("\n"), Does.EndWith("\n  1    7700  A - Microsoft Visual Studio\n  2   16444  B"));
    }
}
