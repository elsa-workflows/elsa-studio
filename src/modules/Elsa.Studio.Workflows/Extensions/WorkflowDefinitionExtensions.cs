using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Extensions;

/// Provides extension methods for the WorkflowDefinition class.
public static class WorkflowDefinitionExtensions
{
    /// <summary>
    /// Determines whether the specified workflow definition is read-only.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to check.</param>
    /// <returns><c>true</c> if the workflow definition is read-only; otherwise, <c>false</c>.</returns>
    public static bool GetIsReadOnly(this WorkflowDefinition? workflowDefinition)
    {
        return workflowDefinition != null && (workflowDefinition.IsLatest == false || (workflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) == 0);
    }
}