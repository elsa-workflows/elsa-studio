namespace Elsa.Studio.Workflows.Contracts;

/// A factory for creating <see cref="IWorkflowInstanceObserver"/> instances.
public interface IWorkflowInstanceObserverFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowInstanceObserver"/> instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to observe.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="IWorkflowInstanceObserver"/> instance.</returns>
    Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId, string observerName, CancellationToken cancellationToken = default);
}