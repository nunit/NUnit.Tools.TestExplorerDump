# te-dump: Test Explorer as text

`te-dump` shows what the Visual Studio Test Explorer shows, as plain text. Use it to check how the
NUnit VS adapter presents tests (the tree, the names, the outcomes) without screenshots, and to let
an AI do the checking for you.

It can also drive Visual Studio for you: open a solution, rebuild, run all or some tests, and close
it again.

Windows only. Needs Visual Studio 2022 or later. Nothing is installed into Visual Studio.

## Install

```
dotnet tool install -g NUnit.Tools.TestExplorerDump
```

Update later with `dotnet tool update -g NUnit.Tools.TestExplorerDump`.
Check the installed version with `te-dump --version`.

## Quick start

Open a terminal in a repro folder (one with a `.slnx`, `.sln` or `.csproj`) and run:

```
te-dump session
```

This starts a new Visual Studio, rebuilds, prints the tree Test Explorer shows after discovery,
runs all tests, prints the tree again, and closes Visual Studio. It takes a minute or two.

```
# discovered
Project: Issue1408 [Not Run] (3)
  Namespace: NUnitTestExplorerFailure [Not Run] (3)
    Class: Tests [Not Run] (3)
      Test Group: Demonstration [Not Run] (2)
        Demonstration([(0, 0)]) [Not Run]
        Demonstration([(1, 2), (3, 4)]) [Not Run]
      Demonstration [Not Run]

# after run
Project: Issue1408 [Passed] (3)
  ...
```

Each line is `Kind: Name [Outcome] (test count)`. Leaf tests have no kind and no count.

## What you can do

### Look at a Visual Studio you already have open

```
te-dump                    Print the tree of the only Visual Studio with Test Explorer open
te-dump list               List all of them, numbered
te-dump 2                  Print the tree of number 2 from that list
te-dump --title Issue489   Pick by window title instead
te-dump 2 --expand-all     Expand collapsed nodes first, so nothing is hidden
```

### Run one full check

```
te-dump session                        In the current folder
te-dump session C:\repos\Issue1408     Or give a folder, .slnx, .sln or .csproj
te-dump session --filter SimpleTest    Run only some tests
```

### Work step by step, keeping Visual Studio open

Use this when you want to run several tests one at a time, or look at the tree between runs.

```
te-dump start                              Prints: Started Visual Studio, pid 73760. ...
te-dump --pid 73760 --expand-all           Print the tree
te-dump run --pid 73760 --filter SimpleTest
te-dump --pid 73760 --expand-all           Print it again
te-dump build --pid 73760                  Rebuild after changing the repro or the adapter
te-dump run --pid 73760                    Run all tests
te-dump close --pid 73760                  Close Visual Studio
```

Keep the pid that `start` prints. All later commands take it with `--pid`.

## Commands

| Command   | What it does                                                                                       |
|-----------|----------------------------------------------------------------------------------------------------|
| `dump`    | Print the Test Explorer tree. This is the default, so `te-dump` alone means `te-dump dump`.       |
| `list`    | List Visual Studio instances with Test Explorer open, numbered.                                    |
| `start`   | Start a new Visual Studio on a solution, open Test Explorer, rebuild, wait for discovery, print the pid. |
| `build`   | Rebuild the solution and wait for test discovery.                                                  |
| `run`     | Run all tests, or the tests matching `--filter`, and wait until done.                             |
| `close`   | Close Visual Studio.                                                                               |
| `session` | `start`, `dump`, `run`, `dump` again, `close`. Visual Studio is closed even if a step fails.       |

`start` and `session` always rebuild. Discovery only happens after a build that changes the test
assemblies, so a rebuild is the only way to be sure Test Explorer shows the current code.

## Choosing a Visual Studio

`dump`, `build`, `run` and `close` work on the only Visual Studio with Test Explorer open. When
there are several, pick one:

| Option           | Meaning                                                    |
|------------------|------------------------------------------------------------|
| `<number>`       | Number from the last `te-dump list`.                       |
| `--pid <id>`     | Process id, as printed by `start`.                         |
| `--title <text>` | Part of the window title, usually the solution name.       |

## Options

| Option               | Meaning                                                                                |
|----------------------|----------------------------------------------------------------------------------------|
| `--expand-all`       | Expand collapsed nodes before reading. This changes the view in Visual Studio.         |
| `--filter <text>`    | Test Explorer search text, as typed in its search box. Example: `SimpleTest`.          |
| `--format json`      | JSON instead of text.                                                                  |
| `--out <folder>`     | Where to write dump files. Default: a `te-dump` folder next to the solution.           |
| `-s`, `--short`      | Leave the help text out when several instances match.                                  |
| `--help`, `-h`, `-?` | Show the help.                                                                         |
| `--version`          | Show the version.                                                                      |
| `--readme`           | Open this page in Microsoft Edge.                                                      |

## Dump files

`dump` and `session` also save each tree to a file:

```
<solution folder>\te-dump\<Solution>-<yyyyMMdd-HHmmss>-<snapshot>.txt
```

`<snapshot>` is `dump`, `discovered` or `after-run`. Example:
`Issue1408\te-dump\Issue1408-20261026-193445-after-run.txt`. Older files are kept, so you can diff
a run before and after an adapter change. Add `te-dump/` to the repo's `.gitignore`.

## Good to know

- Trees go to stdout, progress and errors to stderr. Exit code 0 is success, 1 is failure,
  2 is a wrong argument.
- `start` and `session` use the newest stable Visual Studio installed.
- Before a run, outcomes can come from an earlier run. Visual Studio remembers them in its `.vs` folder.
- Without `--expand-all`, `dump` leaves out the children of collapsed nodes. `session` always expands.
- Kinds and outcomes are Visual Studio's own texts, so they follow its display language.
- Durations are left out on purpose, so dumps can be diffed.

## For developers

Building, testing and releasing the tool: see [BUILDING.md](BUILDING.md).

## License

MIT. Copyright (c) 2026 Terje Sandstrom.
