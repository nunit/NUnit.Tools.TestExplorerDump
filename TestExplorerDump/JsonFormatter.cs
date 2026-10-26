using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestExplorerDump;

public class JsonFormatter : ITreeFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Write(IReadOnlyList<TestNode> roots, TextWriter output) =>
        output.WriteLine(JsonSerializer.Serialize(roots, SerializerOptions));

    public void Write(IReadOnlyList<TreeSnapshot> snapshots, TextWriter output) =>
        output.WriteLine(JsonSerializer.Serialize(snapshots, SerializerOptions));
}
