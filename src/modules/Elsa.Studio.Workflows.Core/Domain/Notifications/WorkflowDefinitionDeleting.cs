using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionDeleting(string WorkflowDefinitionId) : INotification;