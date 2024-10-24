using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents a notification sent when a workflow definition is about to be published by its ID.
/// </summary>
public record WorkflowDefinitionIdPublishing(string WorkflowDefinitionId) : INotification;