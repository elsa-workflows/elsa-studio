namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Local page state for the server log viewer.
/// </summary>
public class ServerLogViewState
{
    public ServerLogFilter Filter { get; set; } = new();
    public bool IsPaused { get; set; }
    public bool AutoScroll { get; set; } = true;
    public bool WrapMessages { get; set; } = true;
    public bool Compact { get; set; } = true;
    public ServerLogConnectionStatus ConnectionStatus { get; set; } = ServerLogConnectionStatus.Disconnected;
    public int VisibleRowCap { get; set; } = 1000;
    public int LocalDroppedRows { get; set; }
}
