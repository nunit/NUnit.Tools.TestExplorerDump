# NUnit.Tools.TestExplorerDump

`te-dump` prints the Visual Studio Test Explorer tree as text, so an AI (or a diff) sees exactly
what Test Explorer shows. It can also drive Visual Studio: start it on a solution, build, run all
or some tests, and close it. Built for verifying changes to the NUnit VS adapter on small repro
solutions.

It reads Test Explorer through Windows UI Automation and drives Visual Studio through EnvDTE.
Nothing is installed into Visual Studio. Windows only.

## Install

```
dotnet tool install -g NUnit.Tools.TestExplorerDump
```

From a local build, run from the repo root (pack writes to `nupkg/`):

```
dotnet pack TestExplorerDump -c Release
dotnet tool update -g NUnit.Tools.TestExplorerDump --add-source nupkg --prerelease
```

## Commands

```
te-dump [dump] [<target>] [--expand-all] [--format text|json] [-s|--short]
te-dump list
te-dump start [<folder|.slnx|.sln|.csproj>]
te-dump build [<target>]
te-dump run [<target>] [--filter <search text>]
te-dump close [<target>]
te-dump session [<folder|.slnx|.sln|.csproj>] [--filter <search text>] [--format text|json]
```

| Command   | What it does                                                                                       |
|-----------|----------------------------------------------------------------------------------------------------|
| `dump`    | Print the Test Explorer tree. Default command.                                                     |
| `list`    | List Visual Studio instances with Test Explorer open, numbered, and save the list.                |
| `start`   | Start a new Visual Studio on the solution (default: current folder), open Test Explorer, wait for discovery, print the pid. |
| `build`   | Build the solution and wait for test discovery.                                                    |
| `run`     | Run all tests, or the tests Test Explorer shows for `--filter`, and wait until done.              |
| `close`   | Quit Visual Studio.                                                                                |
| `session` | `start`, `build`, `dump`, `run`, `dump` again, `close`. Visual Studio is closed even if a step fails. |

A target picks the Visual Studio instance. Without one, the only instance with Test Explorer open
is used.

| Target           | Meaning                                                    |
|------------------|------------------------------------------------------------|
| `<number>`       | Instance number from the last `te-dump list`.              |
| `--pid <id>`     | Process id of the `devenv.exe`.                            |
| `--title <text>` | Part of the window title, usually the solution name.       |

| Option         | Meaning                                                                                     |
|----------------|---------------------------------------------------------------------------------------------|
| `--expand-all` | Expand collapsed nodes before reading. This changes the Test Explorer view.                 |
| `--format`     | `text` (default) or `json`.                                                                 |
| `--filter`     | Test Explorer search text, for example `Class:"Class1"` or `FullName:"Ns.Class1.Test"`.     |
| `-s, --short`  | Leave the usage text out of the instance list shown when several instances match.           |

The list is saved to `%TEMP%\te-dump-instances.txt`, sorted by pid, so `te-dump 2` reads instance 2.

Trees go to stdout. Progress and errors go to stderr.
Exit codes: 0 ok, 1 failure (no or several matching instances, timeout, build failed), 2 bad arguments.

## Examples

Full round trip in a repro folder:

```
> cd Issue489
> te-dump session
# discovered
Project: Issue489 [Not Run] (9)
  Namespace: NUnitSetName [Not Run] (9)
    Class: Class1 [Not Run] (9)
      SimpleTest [Not Run]
      ...

# after run
Project: Issue489 [Passed] (9)
  ...
```

Step by step, keeping Visual Studio open for more checks:

```
> te-dump start
Started Visual Studio, pid 73760. Use --pid 73760 with the other commands.
> te-dump --pid 73760 --expand-all
> te-dump run --pid 73760 --filter SimpleTest
> te-dump --pid 73760 --expand-all
> te-dump close --pid 73760
```

Each tree line is `Kind: Name [Outcome] (test count)`. Leaf tests have no kind and no count.

## Limits

- `start` and `session` use the latest stable Visual Studio that `vswhere` finds.
- Outcomes before a run can come from an earlier run: Visual Studio keeps results in its `.vs` folder.
- Without `--expand-all`, `dump` leaves out children of collapsed nodes. `session` always expands.
- Kind and outcome texts are Visual Studio UI texts, so they follow the Visual Studio display language.
- Durations are left out on purpose, so two dumps can be diffed.

## Release

Versions come from git tags through [MinVer](https://github.com/adamralph/minver).
Tag the commit (for example `v1.0.0`), push the tag, then run the
`TestExplorerDump.NuGet.Publish` workflow on that tag.

## License

MIT. Copyright (c) 2026 Terje Sandstrom.
