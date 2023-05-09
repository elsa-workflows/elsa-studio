namespace Elsa.Dashboard.Workflows.Models;

public record WorkflowDefinition(string Id, string DefinitionId, int Version, string Name, string? Description);