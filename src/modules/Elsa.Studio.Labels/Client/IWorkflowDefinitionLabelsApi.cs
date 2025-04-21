using Refit;

namespace Elsa.Studio.Labels.Client;

public interface IWorkflowDefinitionLabelsApi
{
    [Get("/workflow-definitions/{id}/labels")]
    Task<Elsa.Labels.Endpoints.WorkflowDefinitionLabels.List.Response> ListAsync( string Id, CancellationToken cancellationToken = default);

    [Post("/workflow-definitions/{id}/labels")]
    Task<Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update.Response> UpdateAsync(string id, [Body] Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update.Request request, CancellationToken cancellationToken = default);
}
