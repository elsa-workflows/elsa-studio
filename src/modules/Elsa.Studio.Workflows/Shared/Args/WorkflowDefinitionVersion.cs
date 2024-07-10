using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Shared.Args;

public record WorkflowDefinitionVersion(string WorkflowDefinitionId, string WorkflowDefinitionVersionId, int Version)
{
    public static WorkflowDefinitionVersion FromDefinition(WorkflowDefinition workflowDefinition)
    {
        return new(workflowDefinition.DefinitionId, workflowDefinition.Id, workflowDefinition.Version);
    }
    
    public static WorkflowDefinitionVersion FromDefinitionSummary(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        return new(workflowDefinitionSummary.DefinitionId, workflowDefinitionSummary.Id, workflowDefinitionSummary.Version);
    }
}