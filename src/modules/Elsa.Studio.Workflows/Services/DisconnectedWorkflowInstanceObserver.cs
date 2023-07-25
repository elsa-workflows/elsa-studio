using Elsa.Api.Client.RealTime.Messages;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowInstanceObserver"/> that does nothing.
/// </summary>
public class DisconnectedWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated = default!;
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated = default!;
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated = default!;
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}