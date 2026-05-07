namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Recent log events returned by the backend.
/// </summary>
public class RecentServerLogsResult
{
    public ICollection<ServerLogEvent> Items { get; set; } = new List<ServerLogEvent>();
    public int DroppedEventCount { get; set; }
}
