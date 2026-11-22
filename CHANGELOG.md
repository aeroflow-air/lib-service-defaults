# Changelog

## 0.1.1 — unreleased

Patch release so PackageReference consumers automatically get the logging analysers that 0.1.0 already packed.

### Fixed

- `Microsoft.CodeAnalysis.BannedApiAnalyzers` 3.3.4 now flows as a package dependency (`PrivateAssets=none`, `IncludeAssets=analyzers;build;buildtransitive`). 0.1.0 already shipped `build/AeroFlow.ServiceDefaults.props`, `buildTransitive/AeroFlow.ServiceDefaults.props` and `BannedSymbols.txt` (`System.Console`, `Debug.WriteLine`), and already raised CA1848 / CA2254 / RS0030 as errors in those props. A `PackageReference` added only from the props file is visible to MSBuild but is not restored, so a consumer `Console.WriteLine` still compiled under 0.1.0. The package's own props no longer add that `PackageReference` (it would also duplicate the csproj item when packing this repo).
- A packaging test packs the nupkg into a temporary feed and asserts that a throwaway consumer with `Console.WriteLine` fails the build with RS0030, and that a clean consumer still builds.

Tag `v0.1.1` after this is merged to publish. Do not move or retag `v0.1.0`.

## 0.1.0 — 2026-11-21

First release of `AeroFlow.ServiceDefaults` and `AeroFlow.ServiceDefaults.Testing` to GitHub Packages.
