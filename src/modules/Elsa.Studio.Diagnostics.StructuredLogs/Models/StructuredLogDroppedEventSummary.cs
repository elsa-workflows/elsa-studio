namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Summary emitted when the backend drops structured log events under pressure.
/// </summary>
public class StructuredLogDroppedEventSummary
{
    public int Count { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
