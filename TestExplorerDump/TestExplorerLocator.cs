using System.Diagnostics;
using Interop.UIAutomationClient;

namespace TestExplorerDump;

/// <summary>Finds the Visual Studio instances that have Test Explorer open and match the target options.</summary>
public class TestExplorerLocator(IUIAutomation automation, Options options)
{
    // Test Explorer tool window GUID. Same in every VS display language, unlike the pane name.
    private const string TestExplorerId = "ST:0:0:{e1b7d1f8-9b3c-49b1-8f4f-bfc63a88835d}";

    public TestExplorerLocator ForProcess(int processId) => new(automation, options with { ProcessId = processId, Title = null });

    public TestExplorerWindow OpenWindow(bool expandAll) => new(automation, this, expandAll);

    // An explicit pid needs no lookup, so build, run and close also work while Test Explorer is closed.
    public int FindProcessId() => options.ProcessId ?? FindOne().Instance.ProcessId;

    public IReadOnlyList<TestExplorerPane> FindAll()
    {
        // Process.MainWindowHandle often points at a hidden VS window, so walk the top-level windows instead.
        var devenvIds = Process.GetProcessesByName("devenv").Select(p => p.Id).ToHashSet();
        var windows = automation.GetRootElement().FindAll(TreeScope.TreeScope_Children, automation.CreateTrueCondition());
        var condition = automation.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, TestExplorerId);
        return Enumerable.Range(0, windows.Length)
            .Select(windows.GetElement)
            .Where(w => devenvIds.Contains(w.CurrentProcessId) && options.Matches(w.CurrentProcessId, w.CurrentName))
            .Select(w => (Window: w, Pane: w.FindFirst(TreeScope.TreeScope_Descendants, condition)))
            .Where(m => m.Pane is not null)
            .Select(m => new TestExplorerPane(new VsInstance(m.Window.CurrentProcessId, m.Window.CurrentName), m.Pane))
            .OrderBy(p => p.Instance.ProcessId) // stable numbering between listings
            .ToList();
    }

    public TestExplorerPane FindOne()
    {
        var panes = FindAll();
        return panes.Count switch
        {
            1 => panes[0],
            0 => throw new InvalidOperationException("No matching Visual Studio instance has Test Explorer open."),
            _ => throw new AmbiguousInstanceException(panes.Select(p => p.Instance).ToList()),
        };
    }
}
