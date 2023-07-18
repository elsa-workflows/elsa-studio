using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionsBulkRetracted(ICollection<string> WorkflowDefinitionIds) : INotification;