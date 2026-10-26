using System.Runtime.InteropServices;
using System.Text;
using Interop.UIAutomationClient;
using TestExplorerDump;

Console.OutputEncoding = Encoding.UTF8;
if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("te-dump needs Windows and Visual Studio.");
    return 1;
}

var instanceList = new InstanceListFile(InstanceListFile.DefaultPath);
var writers = new ConsoleWriters(Console.Out, Console.Error);
Options? options = null;
try
{
    options = Options.Parse(args).ResolveNumber(instanceList);
    ITreeFormatter formatter = options.Format == OutputFormat.Json ? new JsonFormatter() : new TextFormatter();
    var locator = new TestExplorerLocator(new CUIAutomation8(), options);
    var registry = new DteRegistry();
    var launcher = new VisualStudioLauncher(registry, new SolutionLocator());
    ICommand command = options.Command switch
    {
        Command.List => new ListCommand(locator, instanceList, writers.Out),
        Command.Start => new StartCommand(launcher, options.Path!, writers),
        Command.Build => new BuildCommand(registry.Get(locator.FindProcessId())),
        Command.Run => new RunCommand(registry.Get(locator.FindProcessId()), locator.OpenWindow(expandAll: false), options.Filter),
        Command.Close => new CloseCommand(registry.Get(locator.FindProcessId())),
        Command.Session => new SessionCommand(launcher, locator, formatter, writers, options.Path!, options.Filter),
        _ => new DumpCommand(locator.OpenWindow(options.ExpandAll), formatter, writers.Out),
    };
    return command.Execute();
}
catch (COMException e)
{
    // UIA_E_ELEMENTNOTAVAILABLE and friends: the tree changed or VS closed while it was read.
    Console.Error.WriteLine($"Automation call failed: {e.Message}");
    return 1;
}
catch (ArgumentException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(Options.Usage);
    return 2;
}
catch (AmbiguousInstanceException e)
{
    instanceList.Save(e.Instances);
    Console.Error.WriteLine(e.Message);
    if (options?.Short != true)
        Console.Error.WriteLine(Environment.NewLine + Options.Usage);
    return 1;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}
