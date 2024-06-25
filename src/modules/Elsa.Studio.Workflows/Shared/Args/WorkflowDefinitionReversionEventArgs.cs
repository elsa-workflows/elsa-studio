namespace Elsa.Studio.Workflows.Shared.Args;

public record WorkflowDefinitionVersionEventArgs(string WorkflowDefinitionVersionId, string WorkflowDefinitionId, int Version);