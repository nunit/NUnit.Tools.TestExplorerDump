namespace TestExplorerDump;

/// <summary>
/// Remembers the numbered instance list from the last listing, one pid per line, so the user can
/// run "te-dump 2" instead of typing a pid.
/// </summary>
public class InstanceListFile(string path)
{
    public static string DefaultPath => Path.Combine(Path.GetTempPath(), "te-dump-instances.txt");

    public void Save(IReadOnlyList<VsInstance> instances) =>
        File.WriteAllLines(path, instances.Select(i => i.ProcessId.ToString()));

    public int ProcessIdOf(int number)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException("No saved instance list. Run te-dump without arguments first.");

        var lines = File.ReadAllLines(path);
        return number <= lines.Length && int.TryParse(lines[number - 1], out var processId)
            ? processId
            : throw new InvalidOperationException($"No instance {number} in the last list. Run te-dump to list them again.");
    }
}
