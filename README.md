# lib-service-defaults

Golden-path observability package for AeroFlow .NET services ([ADR-0022](https://github.com/aeroflow-air/platform-handbook/blob/main/docs/decisions/0022-observability-on-the-golden-path.md); [design page](https://github.com/aeroflow-air/platform-handbook/blob/main/docs/design/observability.md)).

It publishes two NuGet packages to GitHub Packages:

| Package | What it is |
| --- | --- |
| `AeroFlow.ServiceDefaults` | `AddServiceDefaults()` / `MapDefaultEndpoints()`, OpenTelemetry, health endpoints, standard HTTP/messaging/failure logs, analyser defaults |
| `AeroFlow.ServiceDefaults.Testing` | `LogCollector` and contract assertions for those platform logs |

Event catalogue, field names and a business-event example: [docs/service-defaults.md](docs/service-defaults.md).

The library was first added in [`template-dotnet-service`](https://github.com/aeroflow-air/template-dotnet-service) ([#21](https://github.com/aeroflow-air/template-dotnet-service/pull/21), [#23](https://github.com/aeroflow-air/template-dotnet-service/pull/23); `main` at `50aac82`). History of the package paths was brought across with `git filter-repo`.

## Consume

Feed URL: `https://nuget.pkg.github.com/aeroflow-air/index.json`.

GitHub Packages for NuGet requires authentication even though this repository is public. Do not put a token in a committed `nuget.config`.

### nuget.config (source only)

In the consuming repo:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="aeroflow-air" value="https://nuget.pkg.github.com/aeroflow-air/index.json" />
  </packageSources>
</configuration>
```

Then add a `PackageReference`:

```xml
<PackageReference Include="AeroFlow.ServiceDefaults" Version="0.1.1" />
```

Test projects that use the contract helper also reference `AeroFlow.ServiceDefaults.Testing` at the same version.

### Consumers' CI

The workflow needs `packages: read` so `GITHUB_TOKEN` can restore from the org feed. Example:

```yaml
permissions:
  contents: read
  packages: read

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: read
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
          source-url: https://nuget.pkg.github.com/aeroflow-air/index.json
        env:
          NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

If the job calls `aeroflow-workflows` `dotnet-ci.yml`, pin `@v0.6.0` or later (current latest is `@v0.7.0`), set `github-packages: true`, and grant `packages: read` on the calling job (required for every `@v0.6.0+` caller, even when the input is false):

```yaml
jobs:
  build-and-test:
    uses: aeroflow-air/aeroflow-workflows/.github/workflows/dotnet-ci.yml@v0.7.0
    with:
      solution: Your.Service.sln
      github-packages: true
    permissions:
      contents: read
      packages: read
```

Existing `@v0.5.0` pins are unchanged and cannot restore this feed.

### Local development

You need a GitHub personal access token with the `read:packages` scope (a classic token, or a fine-grained token with read access to packages in `aeroflow-air`). This repository does not create or store that token for you.

Store it in the user NuGet config, not in the repo:

```bash
dotnet nuget add source https://nuget.pkg.github.com/aeroflow-air/index.json \
  --name aeroflow-air \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_TOKEN \
  --store-password-in-clear-text
```

`--store-password-in-clear-text` is required by the current .NET SDK for this source; the entry goes in your user-level NuGet config (`~/.nuget/NuGet/NuGet.Config`), not the repository file. You can also set `NUGET_AUTH_TOKEN` in the environment if you use `setup-dotnet`'s `source-url` locally.

If you already have the `aeroflow-air` source, update the credentials instead of adding it twice.

For Docker restores, pass the same token as a BuildKit secret and keep the token out of the image. A committed `nuget.config` may use `%NUGET_AUTH_TOKEN%` as a credential placeholder (that is not the token itself):

```bash
export NUGET_AUTH_TOKEN=...   # PAT with read:packages; do not commit this
docker build --secret id=nuget_auth_token,env=NUGET_AUTH_TOKEN -t your-service .
```

```dockerfile
RUN --mount=type=secret,id=nuget_auth_token \
    NUGET_AUTH_TOKEN="$(cat /run/secrets/nuget_auth_token)" \
    dotnet restore Your.Service.sln
```

## Develop

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet test AeroFlow.ServiceDefaults.sln
dotnet pack AeroFlow.ServiceDefaults.sln -c Release -o artifacts
```

## Versioning and release

- Versions follow [SemVer](https://semver.org/). Breaking changes in the package surface or in reserved event ids 1000–3000 bump MAJOR; new backwards-compatible events or APIs bump MINOR; fixes bump PATCH.
- Land changes on `main` through a pull request (CI restores, builds, tests and packs).
- Publishing is tag-triggered. After the publish workflow is on `main`, create an annotated tag `vMAJOR.MINOR.PATCH` on the commit you want to release (for example `v0.1.0`). Do not retag a version that has already been pushed; GitHub Packages versions are immutable (`--skip-duplicate`).
- The publish workflow builds and tests, packs both nupkgs with the version from the tag (the leading `v` is stripped), and pushes them to `nuget.pkg.github.com/aeroflow-air` using `GITHUB_TOKEN` (`packages: write`).
- `v0.1.0` is published. The next release is `v0.1.1`, a patch so PackageReference consumers actually get the banned-API analyser (see CHANGELOG). Do not move or retag `v0.1.0`.

## CI

Pull requests and pushes to `main` run the org reusable workflow [`dotnet-ci.yml@v0.5.0`](https://github.com/aeroflow-air/aeroflow-workflows/blob/v0.5.0/.github/workflows/dotnet-ci.yml) (restore, Release build, test) and a `pack` job that produces both nupkgs as artifacts. Nothing is published until a `v*` tag is pushed.
