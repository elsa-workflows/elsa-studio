namespace Elsa.Studio.Workflows.Domain.Models;

public record WorkflowImportFailure(string ErrorMessage, WorkflowImportFailureType FailureType);