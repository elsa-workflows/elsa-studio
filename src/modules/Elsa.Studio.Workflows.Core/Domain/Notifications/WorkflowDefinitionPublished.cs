using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition is published.
public record WorkflowDefinitionPublished(WorkflowDefinition WorkflowDefinition) : INotification;