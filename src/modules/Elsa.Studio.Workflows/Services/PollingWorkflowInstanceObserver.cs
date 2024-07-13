using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// Observes a workflow instance by periodically polling for updates and raising corresponding events.
public class PollingWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowInstancesApi _api;
    private readonly string _workflowInstanceId;
    private readonly TimeSpan _pollingInterval;
    private DateTime _lastChecked = DateTime.UtcNow;
    private bool _checkForUpdates = true;

    /// Observes a workflow instance by periodically polling for updates and raising corresponding events.
    public PollingWorkflowInstanceObserver(IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider, IWorkflowInstancesApi api, string workflowInstanceId, TimeSpan pollingInterval)
    {
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
        _api = api;
        _workflowInstanceId = workflowInstanceId;
        _pollingInterval = pollingInterval;
        
        _ = Task.Run(GetRecentExecutionRecordsAsync);
    }

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
                var request = new HasJournalUpdateRequest { WorkflowInstanceId = _workflowInstanceId, UpdatesSince = _lastChecked };
                var updateAvailable = await _api.HasJournalUpdates(_workflowInstanceId, request);
                _lastChecked = now;

                if (updateAvailable)
                {
                    if (WorkflowJournalUpdated != null!) await WorkflowJournalUpdated(new WorkflowExecutionLogUpdatedMessage());
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