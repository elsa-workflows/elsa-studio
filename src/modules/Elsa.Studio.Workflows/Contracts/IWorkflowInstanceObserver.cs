using Elsa.Api.Client.RealTime.Messages;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Defines a contract for observing workflow instance events such as execution log updates and instance updates.
/// </summary>
public interface IWorkflowInstanceObserver : IAsyncDisposable
{
    string? Name { get; set; }
    
    /// An event that is raised when the workflow execution log is updated.
    event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated;
    
    /// An event that is raised when the activity execution log is updated.
    event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated;
    
    /// An event that is raised when the workflow instance is updated.
    event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated;
}