namespace TestExplorerDump;

/// <summary>Where commands write: results to Out (for the AI or a diff), progress and hints to Progress.</summary>
public record ConsoleWriters(TextWriter Out, TextWriter Progress);
