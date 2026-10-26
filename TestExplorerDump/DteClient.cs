using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE80;
using OutputWindowPane = EnvDTE.OutputWindowPane;
using vsBuildState = EnvDTE.vsBuildState;

namespace TestExplorerDump;

/// <summary>Drives one Visual Studio instance through EnvDTE. Long waits report to <paramref name="progress"/>.</summary>
public class DteClient(int processId, DTE2 dte, TextWriter progress)
{
    // VS rejects COM calls while it is busy (loading, building, discovering). Those calls are retried.
    private const int CallRejected = unchecked((int)0x80010001);
    private const int RetryLater = unchecked((int)0x8001010A);
    private const int AllSubscribersFailed = unchecked((int)0x80040201); // VS's own event handlers failed during the call
    private const int Failed = unchecked((int)0x80004005);
    private static readonly TimeSpan BusyTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan WorkTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(15);

    // Discovery writes to the Tests pane as it goes. A minute with no new text and no end marker means it is not coming.
    private static readonly TimeSpan DiscoveryIdleLimit = TimeSpan.FromMinutes(1);

    public int ProcessId { get; } = processId;

    public string SolutionPath => Retry("Reading the solution path", () => dte.Solution.FullName);

    public void WaitForSolution() =>
        Retry("Waiting for the solution to open", () => dte.Solution.IsOpen ? true : throw new COMException("Solution not open yet.", CallRejected));

    public void ShowTestExplorer() => Retry("Opening Test Explorer", () => dte.ExecuteCommand("TestExplorer.ShowTestExplorer"));

    /// <summary>
    /// Rebuild, then wait for the test discovery the new assemblies trigger. A rebuild, not a build:
    /// the tool cannot know whether existing outputs are current, and an up-to-date build triggers no discovery.
    /// </summary>
    /// <remarks>
    /// Uses the Build.RebuildSolution command. SolutionBuild.Clean followed by SolutionBuild.Build builds fine,
    /// but Test Explorer then never starts a discovery.
    /// </remarks>
    public void Rebuild()
    {
        // A discovery from loading the solution may still run. Let it end so it is not mistaken for ours.
        WaitFor((log, _) => !log.IsDiscovering, "test discovery at load", DiscoveryIdleLimit);
        var before = TestsLog().DiscoveriesStarted;
        Retry("Starting the rebuild", () => dte.ExecuteCommand("Build.RebuildSolution"));
        WaitForBuild();
        var failedProjects = Retry("Reading the build result", () => dte.Solution.SolutionBuild.LastBuildInfo);
        if (failedProjects != 0)
            throw new InvalidOperationException($"Build failed in {failedProjects} project(s).");
        WaitFor((log, _) => log.DiscoveriesStarted > before && !log.IsDiscovering, "test discovery after rebuild", DiscoveryIdleLimit);
    }

    /// <summary>Runs all tests, or only those Test Explorer shows right now when <paramref name="inViewOnly"/> is set.</summary>
    public void RunTestsAndWait(bool inViewOnly)
    {
        var before = TestsLog().RunsFinished;
        Retry("Starting the test run", () => dte.ExecuteCommand(inViewOnly ? "TestExplorer.RunAllTestsInView" : "TestExplorer.RunAllTests"));
        // No idle limit: a test can run for minutes without writing to the Tests pane.
        WaitFor((log, _) => log.RunsFinished > before && !log.IsDiscovering, "test run", idleLimit: null);
    }

    public void Quit()
    {
        using var process = Process.GetProcessById(ProcessId);
        try
        {
            Retry("Quitting Visual Studio", dte.Quit);
        }
        catch (InvalidOperationException)
        {
            // VS can drop the COM connection while it shuts down.
        }

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
            process.Kill();
    }

    // The pane only exists after the first test activity. Its TextDocument throws E_FAIL until the pane has
    // been shown, so activate it then, and only then: activating fires VS events and is polled every 500 ms.
    private TestsPaneLog TestsLog() => Retry("Reading the Tests output pane", () =>
    {
        // Item("Tests") does not find the pane by name in VS 18, so search the collection.
        var pane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(p => p.Name == "Tests");
        if (pane is null)
            return new TestsPaneLog("");
        try
        {
            return new TestsPaneLog(TextOf(pane));
        }
        catch (COMException e) when (e.HResult == Failed)
        {
            pane.Activate();
            return new TestsPaneLog("");
        }
    });

    private static string TextOf(OutputWindowPane pane)
    {
        var document = pane.TextDocument;
        return document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
    }

    // The command returns before the build runs. Wait for it to start (it may already be done when it is quick), then to end.
    private void WaitForBuild()
    {
        var timer = Stopwatch.StartNew();
        while (BuildState() != vsBuildState.vsBuildStateInProgress && timer.Elapsed < TimeSpan.FromSeconds(10))
            Thread.Sleep(200);
        var nextHeartbeat = Heartbeat;
        while (BuildState() == vsBuildState.vsBuildStateInProgress)
        {
            if (timer.Elapsed > WorkTimeout)
                throw new InvalidOperationException($"Gave up waiting for the build after {WorkTimeout.TotalMinutes:0} minutes.");
            if (timer.Elapsed > nextHeartbeat)
            {
                progress.WriteLine($"Still building ({timer.Elapsed.TotalSeconds:0} s)");
                nextHeartbeat += Heartbeat;
            }

            Thread.Sleep(500);
        }
    }

    private vsBuildState BuildState() => Retry("Reading the build state", () => dte.Solution.SolutionBuild.BuildState);

    // Discovery can run more than once in a row, so "done" must hold for a few seconds.
    // Gives up after WorkTimeout, or earlier when the Tests pane gets no new text for idleLimit.
    private void WaitFor(Func<TestsPaneLog, TimeSpan, bool> done, string what, TimeSpan? idleLimit)
    {
        var timer = Stopwatch.StartNew();
        var doneSince = (TimeSpan?)null;
        var lastText = TestsLog().Text;
        var lastChange = TimeSpan.Zero;
        var nextHeartbeat = Heartbeat;
        while (doneSince is null || timer.Elapsed - doneSince < Settle)
        {
            var log = TestsLog();
            if (log.Text != lastText)
                (lastText, lastChange) = (log.Text, timer.Elapsed);
            var idle = timer.Elapsed - lastChange;
            if (timer.Elapsed > WorkTimeout || (doneSince is null && idle > idleLimit))
                throw new InvalidOperationException(
                    $"Gave up waiting for {what} after {timer.Elapsed.TotalSeconds:0} s, nothing new in the Tests pane for {idle.TotalSeconds:0} s. "
                    + $"Last Tests pane line: {log.LastMarker}");
            if (timer.Elapsed > nextHeartbeat)
            {
                progress.WriteLine($"Still waiting for {what} ({timer.Elapsed.TotalSeconds:0} s). Last Tests pane line: {log.LastMarker}");
                nextHeartbeat += Heartbeat;
            }

            doneSince = done(log, timer.Elapsed) ? doneSince ?? timer.Elapsed : null;
            Thread.Sleep(500);
        }
    }

    private void Retry(string step, Action call) => Retry(step, () => { call(); return true; });

    private T Retry<T>(string step, Func<T> call)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return call();
            }
            catch (COMException e) when (e.HResult is CallRejected or RetryLater or AllSubscribersFailed && timer.Elapsed < BusyTimeout)
            {
                Thread.Sleep(500);
            }
            catch (COMException e)
            {
                throw new InvalidOperationException($"{step} failed: {e.Message} (0x{e.HResult:X8})", e);
            }
        }
    }
}
