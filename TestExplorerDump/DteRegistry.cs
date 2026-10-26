using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE80;

namespace TestExplorerDump;

/// <summary>Finds a running Visual Studio through the DTE object it puts in the Running Object Table.</summary>
public class DteRegistry(TextWriter progress)
{
    private const string MonikerPrefix = "!VisualStudio.DTE.";

    public DteClient Get(int processId) =>
        Find(processId) is { } dte
            ? new DteClient(processId, dte, progress)
            : throw new InvalidOperationException($"No Visual Studio automation object for pid {processId}. Is it a running Visual Studio?");

    // A freshly started VS registers its DTE some seconds after the process starts.
    public DteClient WaitFor(int processId, TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < timeout)
        {
            if (Find(processId) is { } dte)
                return new DteClient(processId, dte, progress);
            Thread.Sleep(1000);
        }

        throw new InvalidOperationException($"Visual Studio (pid {processId}) did not start within {timeout.TotalSeconds:0} seconds.");
    }

    private DTE2? Find(int processId)
    {
        Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out var table));
        Marshal.ThrowExceptionForHR(CreateBindCtx(0, out var bindContext));
        table.EnumRunning(out var monikers);
        var moniker = new IMoniker[1];
        while (monikers.Next(1, moniker, IntPtr.Zero) == 0)
        {
            // Display name looks like "!VisualStudio.DTE.18.0:7700".
            moniker[0].GetDisplayName(bindContext, null, out var name);
            if (name.StartsWith(MonikerPrefix, StringComparison.Ordinal) && name.EndsWith($":{processId}", StringComparison.Ordinal))
            {
                table.GetObject(moniker[0], out var dte);
                return (DTE2)dte;
            }
        }

        return null;
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);
}
