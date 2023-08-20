using System.Net;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow instance service that uses a remote backend to retrieve workflow instances.
/// </summary>
public class RemoteWorkflowInstanceService : IWorkflowInstanceService
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowInstanceService"/> class.
    /// </summary>
    public RemoteWorkflowInstanceService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.ListAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await api.DeleteAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkDeleteWorkflowInstancesRequest
        {
            Ids = instanceIds.ToList()
        };
        var api = await GetApiAsync(cancellationToken);
        await api.BulkDeleteAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await GetApiAsync(cancellationToken);
            return await api.GetAsync(id, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default)
    {
        var request = new GetFilteredJournalRequest
        {
            Filter = filter
        };
        var api = await GetApiAsync(cancellationToken);
        return await api.GetFilteredJournalAsync(instanceId, request, skip, take, cancellationToken);
    }
    
    private async Task<IWorkflowInstancesApi> GetApiAsync(CancellationToken cancellationToken = default) => await _remoteBackendApiClientProvider.GetApiAsync<IWorkflowInstancesApi>(cancellationToken);
}