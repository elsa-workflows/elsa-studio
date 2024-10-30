namespace Elsa.Studio.Workflows.Domain.Models;

public record ExecuteWorkflowResult(string? WorkflowInstanceId, bool CannotStart);