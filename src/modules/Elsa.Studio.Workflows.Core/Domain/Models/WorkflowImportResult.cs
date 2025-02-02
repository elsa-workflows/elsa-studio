using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

public class WorkflowImportResult
{
    public string FileName { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }
    public WorkflowImportFailure? Failure { get; set; }
    public bool IsSuccess => Failure == null;
}