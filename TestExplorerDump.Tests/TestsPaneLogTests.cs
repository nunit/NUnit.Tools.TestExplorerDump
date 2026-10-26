namespace TestExplorerDump.Tests;

[TestFixture]
public class TestsPaneLogTests
{
    private const string LoadThenRun = """
        ========== Starting test discovery ==========
        NUnit Adapter 6.4.0.0: Test discovery starting
        ========== Test discovery finished: 6 Tests found in 3.1 sec ==========
        ========== Starting test discovery ==========
        ========== Test discovery skipped: All test containers are up to date ==========
        ========== Starting test run ==========
        ========== Test run finished: 4 Tests (4 Passed, 0 Failed, 0 Skipped) run in 393 ms ==========
        """;

    [Test]
    public void Counts_FinishedAndSkippedDiscoveriesAndRuns()
    {
        var log = new TestsPaneLog(LoadThenRun);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(log.DiscoveriesStarted, Is.EqualTo(2));
            Assert.That(log.DiscoveriesFinished, Is.EqualTo(2));
            Assert.That(log.RunsFinished, Is.EqualTo(1));
            Assert.That(log.IsDiscovering, Is.False);
        }
    }

    [Test]
    public void IsDiscovering_StartWithoutEnd_IsTrue()
    {
        var log = new TestsPaneLog("========== Starting test discovery ==========\nNUnit Adapter: Test discovery starting\n");

        Assert.That(log.IsDiscovering, Is.True);
    }

    [Test]
    public void EmptyPane_CountsNothing()
    {
        var log = new TestsPaneLog("");

        Assert.That(new[] { log.DiscoveriesStarted, log.DiscoveriesFinished, log.RunsFinished }, Is.All.Zero);
    }
}
