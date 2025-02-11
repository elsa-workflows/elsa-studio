using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

public class ImportOptions
{
    public int MaxAllowedSize { get; set; } = 1024 * 1024 * 10; // 10 MB
    public string? DefinitionId { get; set; }
    public Func<WorkflowDefinition, Task>? ImportedCallback { get; set; }
    public Func<Exception, Task> ErrorCallback { get; set; }
}