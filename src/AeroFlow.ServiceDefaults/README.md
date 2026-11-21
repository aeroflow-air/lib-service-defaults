# AeroFlow.ServiceDefaults

Golden-path observability for AeroFlow .NET services ([ADR-0022](https://github.com/aeroflow-air/platform-handbook/blob/main/docs/decisions/0022-observability-on-the-golden-path.md)). Promoted from `AeroFlow.Local.ServiceDefaults` in aeroflow-local.

This package is published from [`lib-service-defaults`](https://github.com/aeroflow-air/lib-service-defaults) to GitHub Packages. How to add the feed, authenticate, and reference a version is in that repository's [README](https://github.com/aeroflow-air/lib-service-defaults#consume).

## What it provides

Call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`:

- OpenTelemetry traces, metrics and logs, with required resource attributes (`service.name`, `service.version`, `deployment.environment`, `aeroflow.squad`)
- OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; Azure Monitor when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set
- The Azure SDK activity source enabled, so Service Bus can carry `Diagnostic-Id`
- `/health` (readiness) and `/alive` (liveness) in every environment, excluded from traces and request logs
- HTTP resilience and service discovery (the same defaults as the Aspire ServiceDefaults project)
- **Standard logs from the package itself**: every HTTP request, every message published or consumed (via `IObservabilityLog`), every failure — all with the same fields. Teams only add business events.

Analyser defaults also flow to consumers: CA1848 and CA2254 as errors, and `System.Console` banned in favour of `ILogger` / `[LoggerMessage]`.

See [docs/service-defaults.md](https://github.com/aeroflow-air/lib-service-defaults/blob/main/docs/service-defaults.md) for the event catalogue, field names, configuration and a business-event example. The contract-test helper is `AeroFlow.ServiceDefaults.Testing`.
