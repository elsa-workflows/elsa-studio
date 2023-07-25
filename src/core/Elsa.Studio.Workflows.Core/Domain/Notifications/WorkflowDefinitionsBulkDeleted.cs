using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionsBulkDeleted(ICollection<string> WorkflowDefinitionIds) : INotification;