using Elsa.Studio.ServerLogs.Models;

namespace Elsa.Studio.ServerLogs.Contracts;

/// <summary>
/// Observes live server log events from the active backend.
/// </summary>
public interface IServerLogObserver : IAsyncDisposable
{
    event Func<ServerLogEvent, Task>? LogReceived;
    event Func<ServerLogDroppedEventSummary, Task>? DroppedEventsReceived;
    event Func<ServerLogSource, Task>? SourceChanged;
    event Func<ServerLogConnectionStatus, Task>? ConnectionStatusChanged;

    Task StartAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    Task UpdateFilterAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    Task ReconnectAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
}
