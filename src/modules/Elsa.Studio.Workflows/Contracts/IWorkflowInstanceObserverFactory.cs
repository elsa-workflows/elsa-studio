namespace Elsa.Studio.Workflows.Contracts;

public interface IWorkflowInstanceObserverFactory
{
    Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
}