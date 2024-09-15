using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity execution service that uses a remote backend to retrieve activity execution reports.
/// </summary>
public class RemoteActivityExecutionService : IActivityExecutionService
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteActivityRegistryProvider"/> class.
    /// </summary>
    public RemoteActivityExecutionService(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default)
    {
        var activityNodeIds = containerActivity.GetActivities().Select(x => x.GetNodeId()).ToList();
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeIds = activityNodeIds
        };
        var api = await _backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
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
        
        var api = await _backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListAsync(request, cancellationToken);
        return response.Items;
    }
}