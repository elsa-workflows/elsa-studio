using Elsa.Studio.Labels.Models;

namespace Elsa.Studio.Labels.Contracts;

/// <summary>
/// Provides workflow contexts.
/// </summary>
public interface IWorkflowDefinitionLabelsProvider
{
    /// <summary>
    /// Returns a list of workflow context provider descriptors.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of workflow definition label descriptors.</returns>
    Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> ListAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the labels associated with a workflow definition.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <param name="selectedLabelsIds">The IDs of the selected labels to associate with the workflow definition.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of updated workflow definition label descriptors.</returns>
    Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> UpdateAsync(string workflowDefinitionId, IEnumerable<string> selectedLabelsIds, CancellationToken cancellationToken = default);
}
