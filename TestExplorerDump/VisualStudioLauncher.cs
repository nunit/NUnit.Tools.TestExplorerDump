using System.Diagnostics;

namespace TestExplorerDump;

/// <summary>
/// Starts a fresh Visual Studio on a solution, so a newly built test adapter is loaded, opens Test
/// Explorer, rebuilds the solution and waits until the tests are discovered.
/// </summary>
public class VisualStudioLauncher(DteRegistry registry, SolutionLocator solutions)
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);

    public DteClient Start(string path, TextWriter progress)
    {
        var solution = solutions.Find(path);
        progress.WriteLine($"Starting Visual Studio on {solution}");
        var startInfo = new ProcessStartInfo(FindDevenv()) { ArgumentList = { solution } };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start devenv.exe.");
        DteClient? visualStudio = null;
        try
        {
            visualStudio = registry.WaitFor(process.Id, StartTimeout);
            visualStudio.WaitForSolution();
            visualStudio.ShowTestExplorer();
            progress.WriteLine($"Rebuilding and waiting for test discovery (pid {visualStudio.ProcessId})");
            visualStudio.Rebuild();
            return visualStudio;
        }
        catch
        {
            // Do not leave a half started Visual Studio behind.
            if (visualStudio is not null)
                visualStudio.Quit();
            else
                process.Kill();
            throw;
        }
    }

    // shortcut: always the latest stable VS; add a --vs option if prerelease or older versions are needed.
    private static string FindDevenv()
    {
        var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
            throw new InvalidOperationException($"vswhere.exe not found at '{vswhere}'. Is Visual Studio installed?");

        using var process = Process.Start(new ProcessStartInfo(vswhere, "-latest -property productPath")
        {
            RedirectStandardOutput = true,
        })!;
        var devenv = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return File.Exists(devenv) ? devenv : throw new InvalidOperationException("vswhere found no Visual Studio installation.");
    }
}
