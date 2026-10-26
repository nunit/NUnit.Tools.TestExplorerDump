using System.Text;

namespace TestExplorerDump;

/// <summary>Prefixes each progress line with the time, so a caller can see where a slow step is.</summary>
public class TimestampedWriter(TextWriter inner) : TextWriter
{
    public override Encoding Encoding => inner.Encoding;

    public override void WriteLine(string? value)
    {
        inner.WriteLine($"{DateTime.Now:HH:mm:ss} {value}");
        inner.Flush();
    }

    public override void Write(char value) => inner.Write(value);
}
