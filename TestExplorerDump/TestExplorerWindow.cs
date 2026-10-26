using System.Diagnostics;
using Interop.UIAutomationClient;

namespace TestExplorerDump;

/// <summary>
/// Reads and drives the Test Explorer tree of one Visual Studio through the UI Automation COM API.
/// Rows outside the viewport are virtualized, so rows are addressed by index, which realizes them.
/// </summary>
public class TestExplorerWindow(IUIAutomation automation, TestExplorerLocator locator, bool expandAll)
{
    private const string TreeId = "Tracking List View";
    private const string SearchBoxId = "PART_SearchBox";
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(2);

    public IReadOnlyList<TestNode> Read()
    {
        var tree = Tree();
        if (expandAll)
            ExpandAll(tree);
        return TreeBuilder.Build(Rows(tree).Select(ReadCells).ToList());
    }

    // In a freshly started VS the pane, the tree and the rows show up one after another.
    public void WaitForTests()
    {
        var timer = Stopwatch.StartNew();
        while (TryCountRows() == 0)
        {
            if (timer.Elapsed > TimeSpan.FromMinutes(2))
                throw new InvalidOperationException("Test Explorer showed no tests within 2 minutes.");
            Thread.Sleep(500);
        }

        WaitUntilSettled(TryCountRows);
    }

    /// <summary>Types into the Test Explorer search box, so only matching tests are shown. Empty text clears it.</summary>
    public void Search(string text)
    {
        var box = locator.FindOne().Element.FindFirst(TreeScope.TreeScope_Descendants,
                automation.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, SearchBoxId))
            ?? throw new InvalidOperationException("Test Explorer search box not found.");
        ((IUIAutomationValuePattern)box.GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId)).SetValue(text);
        // Test Explorer filters a moment after the text changes.
        Thread.Sleep(1000);
        WaitUntilSettled(TryCountRows);
    }

    private int TryCountRows()
    {
        try
        {
            return GridOf(Tree()).CurrentRowCount;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    private IUIAutomationElement Tree() =>
        locator.FindOne().Element.FindFirst(TreeScope.TreeScope_Descendants,
            automation.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, TreeId))
        ?? throw new InvalidOperationException("Test Explorer has no test tree. Build the solution so tests are discovered.");

    private static IUIAutomationGridPattern GridOf(IUIAutomationElement tree) =>
        (IUIAutomationGridPattern)tree.GetCurrentPattern(UIA_PatternIds.UIA_GridPatternId);

    // Expanding can make VS replace row elements, which breaks iteration, so restart from the top after each expand.
    private void ExpandAll(IUIAutomationElement tree)
    {
        var grid = GridOf(tree);
        var timer = Stopwatch.StartNew();
        while (Rows(tree).Select(ExpandCollapseOf).FirstOrDefault(IsCollapsed) is { } collapsed)
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(30))
                throw new InvalidOperationException("Gave up expanding Test Explorer after 30 seconds.");
            var rowCount = grid.CurrentRowCount;
            collapsed.Expand();
            // Expand returns before VS has inserted the child rows. Expanding the next row while VS is still
            // inserting leaves that row marked Expanded with no children, so also wait for the count to settle.
            WaitUntil(() => grid.CurrentRowCount > rowCount);
            WaitUntilSettled(() => grid.CurrentRowCount);
        }
    }

    // All rows in display order, including virtualized ones. ItemContainerPattern is unreliable here: it often
    // stops at the edge of the viewport. GridPattern.GetItem addresses rows by index and realizes them.
    private IEnumerable<IUIAutomationElement> Rows(IUIAutomationElement tree)
    {
        var grid = GridOf(tree);
        for (var i = 0; i < grid.CurrentRowCount; i++)
        {
            // VS creates the cell elements lazily: GetItem returns null until a client has walked the rows once.
            var cell = grid.GetItem(i, 0) ?? WalkThenGetItem(tree, grid, i)
                ?? throw new InvalidOperationException($"Test Explorer row {i} is not available. Is the Test Explorer tab visible?");
            yield return automation.ControlViewWalker.GetParentElement(cell);
        }
    }

    private IUIAutomationElement? WalkThenGetItem(IUIAutomationElement tree, IUIAutomationGridPattern grid, int row)
    {
        tree.FindAll(TreeScope.TreeScope_Descendants, automation.CreateTrueCondition());
        return grid.GetItem(row, 0);
    }

    private bool IsCollapsed(IUIAutomationExpandCollapsePattern? pattern) =>
        pattern?.CurrentExpandCollapseState == ExpandCollapseState.ExpandCollapseState_Collapsed;

    // The first cell of a row holds the outcome icon, the label and, for groups, the "(n)" count.
    private TreeItemCells ReadCells(IUIAutomationElement row)
    {
        var name = row.CurrentName;
        var expanded = ExpandCollapseOf(row)?.CurrentExpandCollapseState == ExpandCollapseState.ExpandCollapseState_Expanded;

        IUIAutomationElement? cell = null;
        WaitUntil(() => (cell = row.FindFirst(TreeScope.TreeScope_Children, ControlTypeIs(UIA_ControlTypeIds.UIA_CustomControlTypeId)))?
            .FindFirst(TreeScope.TreeScope_Children, ControlTypeIs(UIA_ControlTypeIds.UIA_TextControlTypeId)) is not null);
        if (cell is null)
            return new TreeItemCells(name, name, null, null, expanded);

        var texts = All(cell, TreeScope.TreeScope_Children, ControlTypeIs(UIA_ControlTypeIds.UIA_TextControlTypeId))
            .Select(t => t.CurrentName)
            .ToList();
        var outcome = cell.FindFirst(TreeScope.TreeScope_Children, ControlTypeIs(UIA_ControlTypeIds.UIA_ImageControlTypeId))?.CurrentName;
        return new TreeItemCells(name, texts.FirstOrDefault() ?? name, outcome, texts.Skip(1).FirstOrDefault(), expanded);
    }

    private IUIAutomationExpandCollapsePattern? ExpandCollapseOf(IUIAutomationElement row) =>
        row.GetCurrentPattern(UIA_PatternIds.UIA_ExpandCollapsePatternId) as IUIAutomationExpandCollapsePattern;

    private IEnumerable<IUIAutomationElement> All(IUIAutomationElement parent, TreeScope scope, IUIAutomationCondition condition)
    {
        var found = parent.FindAll(scope, condition);
        for (var i = 0; i < found.Length; i++)
            yield return found.GetElement(i);
    }

    private IUIAutomationCondition ControlTypeIs(int controlTypeId) =>
        automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, controlTypeId);

    private void WaitUntil(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < RenderTimeout)
            Thread.Sleep(50);
    }

    private void WaitUntilSettled(Func<int> value)
    {
        int previous;
        do
        {
            previous = value();
            Thread.Sleep(300);
        }
        while (value() != previous);
    }
}
