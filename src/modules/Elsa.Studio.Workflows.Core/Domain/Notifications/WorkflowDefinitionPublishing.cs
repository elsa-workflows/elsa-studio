using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition is about to be published.
public record WorkflowDefinitionPublishing(string WorkflowDefinitionId) : INotification;