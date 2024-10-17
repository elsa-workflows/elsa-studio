using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// A notification sent when a workflow definition failed to publish.
/// </summary>
public record WorkflowDefinitionPublishingFailed(WorkflowDefinition WorkflowDefinition, ValidationErrors Errors) : INotification;