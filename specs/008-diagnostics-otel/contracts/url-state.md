# Contract: URL State

Canonical route:

```text
/diagnostics/opentelemetry
```

Supported query parameters:

| Parameter | Meaning |
|-----------|---------|
| `tab` | `resources`, `traces`, `metrics`, `logs`, or `setup`. |
| `resource` | Resource key. |
| `service` | Service name. |
| `trace` | Trace ID. |
| `span` | Span ID. |
| `workflowInstance` | Workflow instance ID. |
| `workflowDefinition` | Workflow definition ID. |
| `status` | Trace/span status. |
| `severity` | OTLP log severity filter. |
| `text` | Free-text search. |
| `from` | Inclusive UTC start timestamp. |
| `to` | Inclusive UTC end timestamp. |
| `live` | `true` or `false`. |

Examples:

```text
/diagnostics/opentelemetry?tab=traces&workflowInstance=wf-1
/diagnostics/opentelemetry?tab=traces&trace=4bf92f3577b34da6a3ce929d0e0e4736
/diagnostics/opentelemetry?tab=logs&trace=4bf92f3577b34da6a3ce929d0e0e4736&span=00f067aa0ba902b7
```

Cross-module links:

- Structured Logs may link to OpenTelemetry with `trace` and optional `span`.
- OpenTelemetry may link to Structured Logs with matching trace/span query values.
- OpenTelemetry may link to Console Logs with resource/source and time range when a compatible source mapping exists.
- Console Logs links must use that module's public route/query contract only; OpenTelemetry must not depend on Console Logs internals.
