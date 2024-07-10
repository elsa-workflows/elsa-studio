using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsPublished(ICollection<string> WorkflowDefinitionIds) : INotification;