namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// A single server log event received from the backend.
/// </summary>
public class ServerLogEvent
{
    public string Id { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public ServerLogLevel Level { get; set; }
    public string Category { get; set; } = default!;
    public string Message { get; set; } = default!;
    public ServerLogException? Exception { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string SourceId { get; set; } = default!;
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public IDictionary<string, object?> Scopes { get; set; } = new Dictionary<string, object?>();
}
