using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsDeleting(ICollection<string> WorkflowDefinitionIds) : INotification;