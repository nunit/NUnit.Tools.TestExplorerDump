namespace TestExplorerDump;

/// <summary>
/// Text of the Visual Studio "Tests" output pane. Test Explorer writes a marker line when
/// discovery and test runs start and end, so counting the markers tells when work is done.
/// </summary>
/// <remarks>
/// End markers vary: "Test discovery finished: 6 Tests found", "Test discovery skipped: All test
/// containers are up to date", "Test run finished: ...", "Test run aborted ...". Start markers use
/// lower case ("Starting test discovery"), so "========== Test discovery" only matches the end.
/// </remarks>
public record TestsPaneLog(string Text)
{
    public int DiscoveriesStarted => Count("========== Starting test discovery");

    public int DiscoveriesFinished => Count("========== Test discovery ");

    public int RunsFinished => Count("========== Test run ");

    public bool IsDiscovering => DiscoveriesStarted > DiscoveriesFinished;

    private int Count(string marker)
    {
        var count = 0;
        for (var index = Text.IndexOf(marker, StringComparison.Ordinal); index >= 0; index = Text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
