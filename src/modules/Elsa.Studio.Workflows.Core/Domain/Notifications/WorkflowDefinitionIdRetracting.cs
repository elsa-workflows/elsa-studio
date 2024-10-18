using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionIdRetracting(string WorkflowDefinitionId) : INotification;