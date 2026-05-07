using Elsa.Studio.ServerLogs.Models;

namespace Elsa.Studio.ServerLogs.Contracts;

/// <summary>
/// Loads server log data from the active backend.
/// </summary>
public interface IServerLogService
{
    /// <summary>
    /// Gets recent server log events.
    /// </summary>
    Task<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, int rowCap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists server log sources.
    /// </summary>
    Task<ICollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
