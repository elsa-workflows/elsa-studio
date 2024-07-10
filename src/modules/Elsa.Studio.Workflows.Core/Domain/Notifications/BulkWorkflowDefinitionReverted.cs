using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionReverted(ICollection<WorkflowDefinitionVersion> WorkflowDefinitionVersions);