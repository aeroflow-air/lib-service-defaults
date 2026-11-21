# lib-service-defaults

Shared observability package for AeroFlow .NET services ([ADR-0022](https://github.com/aeroflow-air/platform-handbook/blob/main/docs/decisions/0022-observability-on-the-golden-path.md)).

## Conventions

- Domain-facing guidance lives in [docs/service-defaults.md](docs/service-defaults.md): platform event ids stay below 10,000; business events start at 10,000; logs only at the edges; no PII or secrets.
- Consumers call `AddServiceDefaults()` and `MapDefaultEndpoints()`. HTTP request logs are automatic; messaging and unexpected failures go through `IObservabilityLog`.
- CA1848, CA2254 and banned `System.Console` are errors via `build/` / `buildTransitive` props.
- Do not log from tests with `Console`. Package tests use `AeroFlow.ServiceDefaults.Testing`.

## Release

Do not push `v*` tags from everyday work. Versions are published by the tag-triggered workflow; see the [README](README.md#versioning-and-release).
