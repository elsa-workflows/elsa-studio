using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Shared.Args;

public record BulkWorkflowDefinitionVersionEventArgs(ICollection<WorkflowDefinitionVersion> WorkflowDefinitionVersions);