using System.Reflection;

namespace TestExplorerDump;

/// <summary>Prints the package version that MinVer took from the git tag, for example 1.0.0.</summary>
public class VersionCommand(TextWriter output) : ICommand
{
    public int Execute()
    {
        var version = typeof(VersionCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        // The SDK appends "+<commit sha>"; the version alone is what users compare.
        output.WriteLine(version.Split('+')[0]);
        return 0;
    }
}
