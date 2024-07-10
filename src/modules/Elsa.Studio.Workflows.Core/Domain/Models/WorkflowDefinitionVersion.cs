using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

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

    public static WorkflowDefinitionVersion FromDefinitionModel(WorkflowDefinitionModel model)
    {
        return new(model.DefinitionId, model.Id, model.Version);
    }
}