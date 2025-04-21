using Elsa.Labels.Entities;
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
    Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> ListAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> UpdateAsync(string workflowDefinitionId, IEnumerable<string> selectedLabelsIds, CancellationToken cancellationToken = default);
}