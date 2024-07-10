using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionReverting(ICollection<WorkflowDefinitionVersion> WorkflowDefinitionVersions);