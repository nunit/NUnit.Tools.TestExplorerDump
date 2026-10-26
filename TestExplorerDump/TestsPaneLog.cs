namespace TestExplorerDump;

/// <summary>
/// Text of the Visual Studio "Tests" output pane. Test Explorer writes a marker line when
/// discovery and test runs start and end, so the markers tell when work is done.
/// </summary>
/// <remarks>
/// End markers vary: "Test discovery finished: 6 Tests found", "Test discovery skipped: All test
/// containers are up to date", "Test run finished: ...", "Test run aborted ...". Start markers use
/// lower case ("Starting test discovery"), so "========== Test discovery" only matches the end.
/// </remarks>
public record TestsPaneLog(string Text)
{
    private const string DiscoveryStart = "========== Starting test discovery";
    private const string DiscoveryEnd = "========== Test discovery ";
    private const string RunEnd = "========== Test run ";

    public int DiscoveriesStarted => Count(DiscoveryStart);

    public int DiscoveriesFinished => Count(DiscoveryEnd);

    public int RunsFinished => Count(RunEnd);

    // Compares the last start with the last end, not the counts: a discovery that VS cancels can
    // leave a start marker without an end, and counting would then report "discovering" forever.
    public bool IsDiscovering => Text.LastIndexOf(DiscoveryStart, StringComparison.Ordinal) > Text.LastIndexOf(DiscoveryEnd, StringComparison.Ordinal);

    /// <summary>The last "==========" line, for progress and error messages.</summary>
    public string LastMarker =>
        Text.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith("==========", StringComparison.Ordinal)) ?? "(none)";

    private int Count(string marker)
    {
        var count = 0;
        for (var index = Text.IndexOf(marker, StringComparison.Ordinal); index >= 0; index = Text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
