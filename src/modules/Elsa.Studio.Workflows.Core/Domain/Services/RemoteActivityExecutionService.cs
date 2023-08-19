using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity execution service that uses a remote backend to retrieve activity execution reports.
/// </summary>
public class RemoteActivityExecutionService : IActivityExecutionService
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteActivityRegistryProvider"/> class.
    /// </summary>
    public RemoteActivityExecutionService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default)
    {
        var activityIds = containerActivity.GetActivities().Select(x => x.GetId()).ToList();
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityIds = activityIds
        };
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetReportAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = activityId
        };
        
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListAsync(request, cancellationToken);
        return response.Items;
    }
}