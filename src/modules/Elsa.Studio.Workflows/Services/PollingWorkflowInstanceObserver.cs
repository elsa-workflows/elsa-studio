using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Services;

/// Observes a workflow instance by periodically polling for updates and raising corresponding events.
public class PollingWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowInstancesApi _api;
    private readonly string _workflowInstanceId;
    private readonly TimeSpan _pollingInterval;
    private readonly ILogger<PollingWorkflowInstanceObserver> _logger;
    private DateTimeOffset _lastUpdateAt = DateTimeOffset.MinValue;
    private bool _checkForUpdates = true;

    /// Observes a workflow instance by periodically polling for updates and raising corresponding events.
    public PollingWorkflowInstanceObserver(IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider, IWorkflowInstancesApi api, string workflowInstanceId, TimeSpan pollingInterval, ILogger<PollingWorkflowInstanceObserver> logger)
    {
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
        _api = api;
        _workflowInstanceId = workflowInstanceId;
        _pollingInterval = pollingInterval;
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
                var response = await _api.GetUpdatedAtAsync(_workflowInstanceId);
                var lastUpdateAt = response.UpdatedAt;

                if (_lastUpdateAt < lastUpdateAt)
                {
                    _lastUpdateAt = lastUpdateAt;
                    if (WorkflowJournalUpdated != null!) await WorkflowJournalUpdated(new WorkflowExecutionLogUpdatedMessage());
                    //if (ActivityExecutionLogUpdated != null!) await ActivityExecutionLogUpdated(new ActivityExecutionLogUpdatedMessage());
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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _checkForUpdates = false;
        return ValueTask.CompletedTask;
    }
}