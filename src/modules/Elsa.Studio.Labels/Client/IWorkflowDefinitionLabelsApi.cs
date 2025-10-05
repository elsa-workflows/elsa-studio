using Refit;

namespace Elsa.Studio.Labels.Client;

/// <summary>
/// Defines the API for managing workflow definition labels.
/// </summary>
public interface IWorkflowDefinitionLabelsApi
{
    /// <summary>
    /// Retrieves the list of labels associated with a specific workflow definition.
    /// </summary>
    /// <param name="id">The unique identifier of the workflow definition.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the list of labels.</returns>
    [Get("/workflow-definitions/{id}/labels")]
    Task<Elsa.Labels.Endpoints.WorkflowDefinitionLabels.List.Response> ListAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the labels associated with a specific workflow definition.
    /// </summary>
    /// <param name="id">The unique identifier of the workflow definition.</param>
    /// <param name="request">The request object containing the updated labels.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response after updating the labels.</returns>
    [Post("/workflow-definitions/{id}/labels")]
    Task<Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update.Response> UpdateAsync(string id, [Body] Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update.Request request, CancellationToken cancellationToken = default);
}
