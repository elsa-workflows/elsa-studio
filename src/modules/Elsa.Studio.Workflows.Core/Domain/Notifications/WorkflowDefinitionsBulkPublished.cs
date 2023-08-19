using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionsBulkPublished(ICollection<string> WorkflowDefinitionIds) : INotification;