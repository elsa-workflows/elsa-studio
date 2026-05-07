namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Server log query and live subscription filter.
/// </summary>
public class ServerLogFilter
{
    public ServerLogLevel? MinimumLevel { get; set; } = ServerLogLevel.Information;
    public ICollection<ServerLogLevel>? Levels { get; set; }
    public string? CategoryPrefix { get; set; }
    public string? Text { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? Take { get; set; }
}
