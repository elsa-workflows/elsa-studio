using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IWorkflowInstanceService
{
    Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default);
}