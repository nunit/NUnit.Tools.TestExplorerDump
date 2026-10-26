namespace TestExplorerDump;

/// <summary>Finds the solution (or the single project) to open in a folder.</summary>
public class SolutionLocator
{
    public string Find(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
            return fullPath;
        if (!Directory.Exists(fullPath))
            throw new InvalidOperationException($"'{fullPath}' does not exist.");

        var solutions = Directory.GetFiles(fullPath, "*.slnx").Concat(Directory.GetFiles(fullPath, "*.sln")).ToList();
        var candidates = solutions.Count > 0 ? solutions : Directory.GetFiles(fullPath, "*.csproj").ToList();
        return candidates.Count switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException($"No .slnx, .sln or .csproj in '{fullPath}'."),
            _ => throw new InvalidOperationException(
                "More than one candidate. Pass the file to --open:" + Environment.NewLine
                + string.Join(Environment.NewLine, candidates.Select(c => "  " + Path.GetFileName(c)))),
        };
    }
}
