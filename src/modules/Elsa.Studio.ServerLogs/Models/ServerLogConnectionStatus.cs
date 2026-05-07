namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// SignalR and backend availability state for the server log viewer.
/// </summary>
public enum ServerLogConnectionStatus
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Unavailable = 4,
    Unauthorized = 5
}
