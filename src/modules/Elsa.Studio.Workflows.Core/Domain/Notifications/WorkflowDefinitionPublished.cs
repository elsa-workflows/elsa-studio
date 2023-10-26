using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents a notification that is sent when a workflow definition is published.
/// </summary>
public record WorkflowDefinitionPublished(WorkflowDefinition WorkflowDefinition) : INotification;