namespace TestExplorerDump;

/// <summary>The Test Explorer tree at one point of a session, for example after discovery.</summary>
public record TreeSnapshot(string Name, IReadOnlyList<TestNode> Roots);
