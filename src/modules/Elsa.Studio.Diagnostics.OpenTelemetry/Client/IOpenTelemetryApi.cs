using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Client;

/// <summary>
/// Backend API for OpenTelemetry diagnostics.
/// </summary>
public interface IOpenTelemetryApi
{
    [Post("/diagnostics/opentelemetry/resources")]
    Task<OpenTelemetryResourceResult> GetResourcesAsync([Body] OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default);

    [Post("/diagnostics/opentelemetry/traces")]
    Task<OpenTelemetryTraceResult> GetTracesAsync([Body] OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default);

    [Get("/diagnostics/opentelemetry/traces/{traceId}")]
    Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

    [Post("/diagnostics/opentelemetry/metrics")]
    Task<OpenTelemetryMetricResult> GetMetricsAsync([Body] OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default);

    [Post("/diagnostics/opentelemetry/logs")]
    Task<OpenTelemetryLogResult> GetLogsAsync([Body] OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default);

    [Get("/diagnostics/opentelemetry/storage")]
    Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default);

    [Get("/diagnostics/opentelemetry/collector-configuration")]
    Task<CollectorConfiguration> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default);
}
