using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionDeleted(string WorkflowDefinitionVersionId) : INotification;