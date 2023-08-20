using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionVersionDeleted(string WorkflowDefinitionId) : INotification;