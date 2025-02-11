using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsRetracted(ICollection<string> WorkflowDefinitionIds) : INotification;