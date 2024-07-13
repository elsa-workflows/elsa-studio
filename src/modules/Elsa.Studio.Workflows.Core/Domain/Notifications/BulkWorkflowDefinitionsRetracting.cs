using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionsRetracting(ICollection<string> WorkflowDefinitionIds) : INotification;