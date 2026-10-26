# Building te-dump

## Build and test

```
dotnet build
dotnet test
```

The unit tests cover the parts that need no Visual Studio: argument parsing, tree rebuilding,
output formats, dump files. The Visual Studio parts are checked by hand, for example with
`te-dump session` on a repro from the adapter issues repo.

## Install a local build

`dotnet pack` writes the package to `nupkg/` in the repo root:

```
dotnet pack TestExplorerDump -c Release
dotnet tool update -g NUnit.Tools.TestExplorerDump --add-source nupkg --prerelease
```

Without a release tag the version is a prerelease like `0.0.0-alpha.0.3`, hence `--prerelease`.

## How it works

- Test Explorer is read through Windows UI Automation (COM API, `Interop.UIAutomationClient`).
  Rows are read by index through `GridPattern`, and the nesting is rebuilt from the group counts.
- Visual Studio is driven through EnvDTE, found in the Running Object Table by pid.
- Waiting for discovery and test runs reads the marker lines in the "Tests" output pane.
- Rebuild uses the `Build.RebuildSolution` command. `SolutionBuild.Clean` followed by
  `SolutionBuild.Build` compiles too, but Test Explorer then starts no discovery.
- The project targets plain `net10.0`, not `net10.0-windows`, because `PackAsTool` rejects
  platform target frameworks.

## Release

Versions come from git tags through [MinVer](https://github.com/adamralph/minver), tag prefix `v`.

1. Tag the commit and push the tag: `git tag v1.0.0` then `git push origin v1.0.0`.
2. Run the `TestExplorerDump.NuGet.Publish` workflow on that tag (Actions tab, Run workflow).
   It builds, tests, packs and pushes to nuget.org with the `PUBLISH_NUGET_ORG` secret.

CI (`TestExplorerDump.CI`) runs on every push and pull request.
