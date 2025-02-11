using Elsa.Api.Client.RealTime.Messages;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// An implementation of <see cref="IWorkflowInstanceObserver"/> that does nothing.
public class DisconnectedWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    public string? Name { get; set; }

    /// <inheritdoc />
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated = default!;

    /// <inheritdoc />
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated = default!;

    /// <inheritdoc />
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated = default!;
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}