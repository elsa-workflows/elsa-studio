using Elsa.Api.Client.RealTime.Messages;

namespace Elsa.Studio.Workflows.Contracts;

public interface IWorkflowInstanceObserver : IAsyncDisposable
{
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated;
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated;
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated;
}