namespace TestExplorerDump;

/// <summary>Command line options: the command, which Visual Studio it targets and how to print trees.</summary>
/// <remarks>Number picks an instance from the numbered list the last listing printed.</remarks>
public record Options(
    int? ProcessId,
    string? Title,
    bool ExpandAll,
    OutputFormat Format,
    int? Number = null,
    bool Short = false,
    Command Command = Command.Dump,
    string? Path = null,
    string? Filter = null,
    string? Out = null)
{
    public const string Usage = """
        Usage:
          te-dump [dump] [<target>] [--expand-all] [--format text|json] [--out <folder>] [-s|--short]
          te-dump list
          te-dump start [<folder|.slnx|.sln|.csproj>]
          te-dump build [<target>]
          te-dump run [<target>] [--filter <search text>]
          te-dump close [<target>]
          te-dump session [<folder|.slnx|.sln|.csproj>] [--filter <search text>] [--format text|json] [--out <folder>]

        Commands:
          dump          Print the Test Explorer tree. Default command.
          list          List Visual Studio instances with Test Explorer open, numbered.
          start         Start a new Visual Studio on the solution (default: current folder), open Test Explorer,
                        rebuild, wait for test discovery and print the pid.
          build         Rebuild the solution (clean and build) and wait for test discovery.
          run           Run all tests, or the tests Test Explorer shows for --filter, and wait until done.
          close         Quit Visual Studio.
          session       start (with rebuild), dump, run, dump again, close.

        Target (default: the only Visual Studio with Test Explorer open):
          <number>      Instance number from the last list.
          --pid         Process id of the devenv.exe.
          --title       Part of the Visual Studio window title, for example the solution name.

        Options:
          --expand-all  Expand collapsed nodes before reading. This changes the Test Explorer view.
          --format      Output format. Default: text.
          --filter      Test Explorer search text, for example: Class:"Class1" or FullName:"Ns.Class1.Test".
          --out         Folder for dump files. Default: a te-dump folder next to the solution.
          -s, --short   Leave this usage text out of the instance list.

        Dump files: dump and session also write each tree to <Solution>-<yyyyMMdd-HHmmss>-<snapshot>.txt
        (or .json), snapshot being dump, discovered or after-run. The tree is printed to stdout as well.
        """;

    public static Options Parse(IReadOnlyList<string> args)
    {
        var command = args.Count > 0 ? ParseCommand(args[0]) : null;
        int? processId = null;
        string? title = null;
        var expandAll = false;
        var format = OutputFormat.Text;
        int? number = null;
        var isShort = false;
        string? path = null;
        string? filter = null;
        string? outFolder = null;
        for (var i = command is null ? 0 : 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--pid":
                    processId = int.TryParse(ValueOf(args, ref i), out var id)
                        ? id
                        : throw new ArgumentException($"--pid needs a number, got '{args[i]}'.");
                    break;
                case "--title":
                    title = ValueOf(args, ref i);
                    break;
                case "--filter":
                    filter = ValueOf(args, ref i);
                    break;
                case "--out":
                    outFolder = ValueOf(args, ref i);
                    break;
                case "-s" or "--short":
                    isShort = true;
                    break;
                case "--expand-all":
                    expandAll = true;
                    break;
                case "--format":
                    format = ParseFormat(ValueOf(args, ref i));
                    break;
                case var value when command is Command.Start or Command.Session && path is null && !value.StartsWith('-'):
                    path = value;
                    break;
                case var value when number is null && int.TryParse(value, out var n) && n > 0:
                    number = n;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        var options = new Options(processId, title, expandAll, format, number, isShort, command ?? Command.Dump, path, filter, outFolder);
        options.Validate();
        return options.StartsVisualStudio ? options with { Path = path ?? "." } : options;
    }

    public Options ResolveNumber(InstanceListFile instanceList) =>
        Number is { } number ? this with { ProcessId = instanceList.ProcessIdOf(number), Number = null } : this;

    public bool Matches(int processId, string windowTitle) =>
        (ProcessId is null || ProcessId == processId)
        && (Title is null || windowTitle.Contains(Title, StringComparison.OrdinalIgnoreCase));

    private bool StartsVisualStudio => Command is Command.Start or Command.Session;

    private void Validate()
    {
        if (Number is not null && ProcessId is not null)
            throw new ArgumentException("Use either an instance number or --pid, not both.");
        if (StartsVisualStudio && (Number is not null || ProcessId is not null || Title is not null))
            throw new ArgumentException($"{Command.ToString().ToLowerInvariant()} starts its own Visual Studio. Do not give a number, --pid or --title.");
        if (Filter is not null && Command is not (Command.Run or Command.Session))
            throw new ArgumentException("--filter only works with run and session.");
        if (Out is not null && Command is not (Command.Dump or Command.Session))
            throw new ArgumentException("--out only works with dump and session.");
    }

    private static Command? ParseCommand(string value) => value switch
    {
        "dump" => Command.Dump,
        "list" => Command.List,
        "start" => Command.Start,
        "build" => Command.Build,
        "run" => Command.Run,
        "close" => Command.Close,
        "session" => Command.Session,
        _ => null,
    };

    private static string ValueOf(IReadOnlyList<string> args, ref int i) =>
        i + 1 < args.Count ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

    private static OutputFormat ParseFormat(string value) => value.ToLowerInvariant() switch
    {
        "text" => OutputFormat.Text,
        "json" => OutputFormat.Json,
        _ => throw new ArgumentException($"Unknown format '{value}'. Use text or json."),
    };
}
