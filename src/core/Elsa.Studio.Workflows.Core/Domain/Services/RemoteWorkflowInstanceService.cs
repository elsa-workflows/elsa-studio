using System.Net;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

public class RemoteWorkflowInstanceService : IWorkflowInstanceService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public RemoteWorkflowInstanceService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    public async Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider.GetApi<IWorkflowInstancesApi>().ListAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await _backendConnectionProvider.GetApi<IWorkflowInstancesApi>().DeleteAsync(instanceId, cancellationToken);
    }

    public async Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkDeleteWorkflowInstancesRequest
        {
            Ids = instanceIds.ToList()
        };
        await _backendConnectionProvider.GetApi<IWorkflowInstancesApi>().BulkDeleteAsync(request, cancellationToken);
    }

    public async Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _backendConnectionProvider.GetApi<IWorkflowInstancesApi>().GetAsync(id, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PagedListResponse<ExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default)
    {
        var request = new GetFilteredJournalRequest
        {
            Filter = filter
        };
        return await _backendConnectionProvider.GetApi<IWorkflowInstancesApi>().GetFilteredJournalAsync(instanceId, request, skip, take, cancellationToken);
    }
}