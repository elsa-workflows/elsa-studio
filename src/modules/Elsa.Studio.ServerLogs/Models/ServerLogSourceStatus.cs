namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Backend log source health status.
/// </summary>
public enum ServerLogSourceStatus
{
    Unknown = 0,
    Connected = 1,
    Stale = 2,
    Disconnected = 3
}
