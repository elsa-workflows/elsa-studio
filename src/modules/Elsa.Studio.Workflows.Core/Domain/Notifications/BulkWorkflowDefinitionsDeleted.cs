using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsDeleted(ICollection<string> WorkflowDefinitionIds) : INotification;