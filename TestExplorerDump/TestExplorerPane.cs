using Interop.UIAutomationClient;

namespace TestExplorerDump;

/// <summary>The Test Explorer tool window of one Visual Studio instance.</summary>
public record TestExplorerPane(VsInstance Instance, IUIAutomationElement Element);
