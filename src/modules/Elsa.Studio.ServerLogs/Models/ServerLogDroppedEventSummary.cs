namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Summary emitted when the backend drops server log events under pressure.
/// </summary>
public class ServerLogDroppedEventSummary
{
    public int Count { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
