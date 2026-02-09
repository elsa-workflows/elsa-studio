using System.Net;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Resources.Resilience.Contracts;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity execution service that uses a remote backend to retrieve activity execution reports.
/// </summary>
public class RemoteActivityExecutionService(IBackendApiClientProvider backendApiClientProvider) : IActivityExecutionService
{
    /// <inheritdoc />
    public async Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default)
    {
        var activityNodeIds = containerActivity.GetActivities().Select(x => x.GetNodeId()).ToList();
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeIds = activityNodeIds
        };
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetReportAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeId = activityNodeId
        };
        
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListAsync(request, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeId = activityNodeId
        };
        
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListSummariesAsync(request, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await backendApiClientProvider.GetApiAsync<IRetryAttemptsApi>(cancellationToken);
            return await api.ListAsync(activityInstanceId, skip, take, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            // The Resilience feature is not enabled in Elsa Server or the retry attempts endpoint is not available. Return an empty response.
            return new PagedListResponse<RetryAttemptRecord> { Items = [] };
        }
    }
}