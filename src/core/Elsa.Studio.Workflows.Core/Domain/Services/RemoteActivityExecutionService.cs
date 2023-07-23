using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

public class RemoteActivityExecutionService : IActivityExecutionService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public RemoteActivityExecutionService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }
    
    public async Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default)
    {
        var activityIds = containerActivity.GetActivities().Select(x => x.GetId()).ToList();
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityIds = activityIds
        };
        return await _backendConnectionProvider.GetApi<IActivityExecutionsApi>().GetReportAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = activityId
        };
        
        var response = await _backendConnectionProvider.GetApi<IActivityExecutionsApi>().ListAsync(request, cancellationToken);
        return response.Items;
    }
}