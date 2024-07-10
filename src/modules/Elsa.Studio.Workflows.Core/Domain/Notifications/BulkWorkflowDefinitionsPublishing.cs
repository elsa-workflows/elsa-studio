using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsPublishing(ICollection<string> WorkflowDefinitionIds) : INotification;