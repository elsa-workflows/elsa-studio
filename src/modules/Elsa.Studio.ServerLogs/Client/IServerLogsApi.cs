using Elsa.Studio.ServerLogs.Models;
using Refit;

namespace Elsa.Studio.ServerLogs.Client;

/// <summary>
/// Backend API for server log diagnostics.
/// </summary>
public interface IServerLogsApi
{
    /// <summary>
    /// Gets recent log events.
    /// </summary>
    [Post("/server-logs/recent")]
    Task<RecentServerLogsResult> GetRecentAsync([Body] ServerLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists server log sources.
    /// </summary>
    [Get("/server-logs/sources")]
    Task<ICollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
