using Elsa.Api.Client.RealTime.Messages;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Elsa.Studio.Workflows.Services;

/// A wrapper around a SignalR connection that receives notifications about workflow instance updates.
public class RealtimeWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    private readonly HubConnection _connection;

    /// The `RealtimeWorkflowInstanceObserver` class is a wrapper around a SignalR connection that receives notifications about workflow instance updates.
    public RealtimeWorkflowInstanceObserver(HubConnection connection)
    {
        _connection = connection;

        connection.On("ActivityExecutionLogUpdatedAsync", async (ActivityExecutionLogUpdatedMessage message) => await OnActivityExecutionLogUpdatedAsync(message));
        connection.On("WorkflowExecutionLogUpdatedAsync", async (WorkflowExecutionLogUpdatedMessage message) => await OnWorkflowJournalUpdatedAsync(message));
        connection.On("WorkflowInstanceUpdatedAsync", async (WorkflowInstanceUpdatedMessage message) => await OnWorkflowInstanceUpdatedAsync(message));
    }

    /// <inheritdoc />
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated = default!;
    
    /// <inheritdoc />
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated = default!;
    
    /// <inheritdoc />
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated = default!;

    private async Task OnWorkflowJournalUpdatedAsync(WorkflowExecutionLogUpdatedMessage arg)
    {
        if (WorkflowJournalUpdated != null!) await WorkflowJournalUpdated(arg);
    }

    private async Task OnActivityExecutionLogUpdatedAsync(ActivityExecutionLogUpdatedMessage arg)
    {
        if (ActivityExecutionLogUpdated != null!) await ActivityExecutionLogUpdated(arg);
    }

    private async Task OnWorkflowInstanceUpdatedAsync(WorkflowInstanceUpdatedMessage arg)
    {
        if (WorkflowInstanceUpdated != null!) await WorkflowInstanceUpdated(arg);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}