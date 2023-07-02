using System.ComponentModel.DataAnnotations;

namespace Elsa.Studio.Workflows.Models;

public class WorkflowMetadataModel
{
    public string? DefinitionId { get; set; }
    [Required] public string? Name { get; set; }
    public string? Description { get; set; } = default!;
}