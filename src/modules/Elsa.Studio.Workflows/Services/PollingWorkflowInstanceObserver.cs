using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Services;

/// Observes a workflow instance by periodically polling for updates and raising corresponding events.
public class PollingWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowInstancesApi _workflowInstancesApi;
    private readonly IActivityExecutionsApi _activityExecutionsApi;
    private readonly string _workflowInstanceId;
    private readonly JsonObject? _containerActivity;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);
    private readonly ILogger<PollingWorkflowInstanceObserver> _logger;
    private DateTimeOffset _lastUpdateAt = DateTimeOffset.MinValue;
    private bool _checkForUpdates = true;

    /// Observes a workflow instance by periodically polling for updates and raising corresponding events.
    public PollingWorkflowInstanceObserver(
        WorkflowInstanceObserverContext context,
        IBlazorServiceAccessor blazorServiceAccessor,
        IServiceProvider serviceProvider,
        IWorkflowInstancesApi workflowInstancesApi,
        IActivityExecutionsApi activityExecutionsApi,
        ILogger<PollingWorkflowInstanceObserver> logger)
    {
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
        _workflowInstancesApi = workflowInstancesApi;
        _activityExecutionsApi = activityExecutionsApi;
        _workflowInstanceId = context.WorkflowInstanceId;
        _containerActivity = context.ContainerActivity;
        _logger = logger;

        _ = Task.Run(GetRecentExecutionRecordsAsync);
    }

    public string? Name { get; set; }
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated = default!;
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated = default!;
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated = default!;

    private async Task GetRecentExecutionRecordsAsync()
    {
        while (_checkForUpdates)
        {
            var now = DateTime.UtcNow;
            try
            {
                _blazorServiceAccessor.Services = _serviceProvider;
                _logger.LogInformation("{ObserverName} is polling", Name);
                var response = await _workflowInstancesApi.GetUpdatedAtAsync(_workflowInstanceId);
                var lastUpdateAt = response.UpdatedAt;

                if (_lastUpdateAt < lastUpdateAt)
                {
                    _lastUpdateAt = lastUpdateAt;
                    if (WorkflowJournalUpdated != null!) await WorkflowJournalUpdated(new WorkflowExecutionLogUpdatedMessage());
                    if (ActivityExecutionLogUpdated != null && _containerActivity != null)
                    {
                        var activityExecutionStats = await GetActivityExecutionStatsAsync(_containerActivity);
                        await ActivityExecutionLogUpdated(new ActivityExecutionLogUpdatedMessage(activityExecutionStats));
                    }

                    if (WorkflowInstanceUpdated != null!) await WorkflowInstanceUpdated(new WorkflowInstanceUpdatedMessage(_workflowInstanceId));
                }
            }
            catch (ObjectDisposedException)
            {
                _checkForUpdates = false;
                break;
            }

            await Task.Delay(_pollingInterval);
        }
    }

    private async Task<ICollection<ActivityExecutionStats>> GetActivityExecutionStatsAsync(JsonObject container)
    {
        var activityNodeIds = container.GetActivities().Select(x => x.GetNodeId()).ToList(); 
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = _workflowInstanceId, 
            ActivityNodeIds = activityNodeIds,
        };
        var report = await _activityExecutionsApi.GetReportAsync(request);
        return report.Stats;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _checkForUpdates = false;
        return ValueTask.CompletedTask;
    }
}