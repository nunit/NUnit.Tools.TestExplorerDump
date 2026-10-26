using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE80;
using OutputWindowPane = EnvDTE.OutputWindowPane;

namespace TestExplorerDump;

/// <summary>Drives one Visual Studio instance through EnvDTE.</summary>
public class DteClient(int processId, DTE2 dte)
{
    // VS rejects COM calls while it is busy (loading, building). Those calls are retried.
    private const int CallRejected = unchecked((int)0x80010001);
    private const int RetryLater = unchecked((int)0x8001010A);
    private static readonly TimeSpan BusyTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan WorkTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);

    // A build that changed outputs triggers a new discovery a moment later. An up-to-date build triggers none.
    private static readonly TimeSpan DiscoveryStartGrace = TimeSpan.FromSeconds(10);

    public int ProcessId { get; } = processId;

    public void WaitForSolution() =>
        Retry(() => dte.Solution.IsOpen ? true : throw new COMException("Solution not open yet.", CallRejected));

    public void ShowTestExplorer() => Retry(() => dte.ExecuteCommand("TestExplorer.ShowTestExplorer"));

    // Test Explorer discovers tests when the solution loads, but only if there are built test assemblies.
    // A solution that was never built gets no discovery until the first build.
    public void WaitForDiscovery() =>
        WaitFor((log, elapsed) => (log.DiscoveriesStarted > 0 || elapsed > DiscoveryStartGrace) && !log.IsDiscovering, "test discovery");

    public void Build()
    {
        var before = TestsLog().DiscoveriesStarted;
        Retry(() => dte.Solution.SolutionBuild.Build(true));
        var failedProjects = Retry(() => dte.Solution.SolutionBuild.LastBuildInfo);
        if (failedProjects != 0)
            throw new InvalidOperationException($"Build failed in {failedProjects} project(s).");
        WaitFor((log, elapsed) => (log.DiscoveriesStarted > before || elapsed > DiscoveryStartGrace) && !log.IsDiscovering,
            "test discovery after build");
    }

    /// <summary>Runs all tests, or only those Test Explorer shows right now when <paramref name="inViewOnly"/> is set.</summary>
    public void RunTestsAndWait(bool inViewOnly)
    {
        var before = TestsLog().RunsFinished;
        Retry(() => dte.ExecuteCommand(inViewOnly ? "TestExplorer.RunAllTestsInView" : "TestExplorer.RunAllTests"));
        WaitFor((log, _) => log.RunsFinished > before && !log.IsDiscovering, "test run");
    }

    public void Quit()
    {
        using var process = Process.GetProcessById(ProcessId);
        try
        {
            Retry(dte.Quit);
        }
        catch (COMException)
        {
            // VS can drop the COM connection while it shuts down.
        }

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
            process.Kill();
    }

    // The pane only exists after the first test activity. Its TextDocument throws E_FAIL until the pane has been shown.
    private TestsPaneLog TestsLog()
    {
        try
        {
            return Retry(() =>
            {
                // Item("Tests") does not find the pane by name in VS 18, so search the collection.
                var pane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(p => p.Name == "Tests");
                if (pane is null)
                    return new TestsPaneLog("");
                pane.Activate();
                var document = pane.TextDocument;
                return new TestsPaneLog(document.StartPoint.CreateEditPoint().GetText(document.EndPoint));
            });
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x80004005))
        {
            return new TestsPaneLog(""); // E_FAIL: pane exists but has no text buffer yet.
        }
    }

    // Discovery can run more than once in a row, so "done" must hold for a few seconds.
    private void WaitFor(Func<TestsPaneLog, TimeSpan, bool> done, string what)
    {
        var timer = Stopwatch.StartNew();
        var doneSince = (TimeSpan?)null;
        while (doneSince is null || timer.Elapsed - doneSince < Settle)
        {
            var log = TestsLog();
            if (timer.Elapsed > WorkTimeout)
                throw new InvalidOperationException(
                    $"Gave up waiting for {what} after {WorkTimeout.TotalMinutes:0} minutes. Tests pane: {log.DiscoveriesStarted} discoveries started, "
                    + $"{log.DiscoveriesFinished} finished, {log.RunsFinished} runs finished.");
            doneSince = done(log, timer.Elapsed) ? doneSince ?? timer.Elapsed : null;
            Thread.Sleep(500);
        }
    }

    private void Retry(Action call) => Retry(() => { call(); return true; });

    private T Retry<T>(Func<T> call)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return call();
            }
            catch (COMException e) when (e.HResult is CallRejected or RetryLater && timer.Elapsed < BusyTimeout)
            {
                Thread.Sleep(500);
            }
        }
    }
}
