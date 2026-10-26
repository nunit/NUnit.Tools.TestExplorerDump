using System.Text.Json;

namespace TestExplorerDump.Tests;

[TestFixture]
public class JsonFormatterTests
{
    [Test]
    public void Write_LeafNode_OmitsNullsAndKeepsNamesReadable()
    {
        var output = new StringWriter();

        new JsonFormatter().Write([new TestNode(null, "Test<\"x\">", "Passed", null, [])], output);

        var node = JsonDocument.Parse(output.ToString()).RootElement[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.GetProperty("name").GetString(), Is.EqualTo("Test<\"x\">"));
            Assert.That(node.TryGetProperty("kind", out _), Is.False);
            Assert.That(node.TryGetProperty("count", out _), Is.False);
            Assert.That(output.ToString(), Does.Contain("Test<"));
        }
    }
}
