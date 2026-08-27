using Elsa.Api.Client.RealTime.Messages;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// An implementation of <see cref="IWorkflowInstanceObserver"/> that does nothing.
public class DisconnectedWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    private Func<WorkflowExecutionLogUpdatedMessage, Task>? _workflowJournalUpdated;
    private Func<ActivityExecutionLogUpdatedMessage, Task>? _activityExecutionLogUpdated;
    private Func<WorkflowInstanceUpdatedMessage, Task>? _workflowInstanceUpdated;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <inheritdoc />
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated
    {
        add => _workflowJournalUpdated += value;
        remove => _workflowJournalUpdated -= value;
    }

    /// <inheritdoc />
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated
    {
        add => _activityExecutionLogUpdated += value;
        remove => _activityExecutionLogUpdated -= value;
    }

    /// <inheritdoc />
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated
    {
        add => _workflowInstanceUpdated += value;
        remove => _workflowInstanceUpdated -= value;
    }
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
