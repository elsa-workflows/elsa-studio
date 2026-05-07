using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.ServerLogs.Client;
using Elsa.Studio.ServerLogs.Contracts;
using Elsa.Studio.ServerLogs.Models;
using Refit;

namespace Elsa.Studio.ServerLogs.Services;

/// <summary>
/// Loads server logs through the active backend API client.
/// </summary>
public class RemoteServerLogService(IBackendApiClientProvider backendApiClientProvider) : IServerLogService
{
    /// <inheritdoc />
    public async Task<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, int rowCap, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IServerLogsApi>(cancellationToken);
        var request = CopyFilter(filter);
        request.Take = Math.Clamp(request.Take ?? rowCap, 1, rowCap);

        try
        {
            return await api.GetRecentAsync(request, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return new();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IServerLogsApi>(cancellationToken);

        try
        {
            return await api.ListSourcesAsync(cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<ServerLogSource>();
        }
    }

    private static ServerLogFilter CopyFilter(ServerLogFilter filter) =>
        new()
        {
            MinimumLevel = filter.MinimumLevel,
            Levels = filter.Levels?.ToList(),
            CategoryPrefix = filter.CategoryPrefix,
            Text = filter.Text,
            TenantId = filter.TenantId,
            WorkflowDefinitionId = filter.WorkflowDefinitionId,
            WorkflowInstanceId = filter.WorkflowInstanceId,
            TraceId = filter.TraceId,
            CorrelationId = filter.CorrelationId,
            SourceId = filter.SourceId,
            From = filter.From,
            To = filter.To,
            Take = filter.Take
        };
}
