using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition has been saved.
public record WorkflowDefinitionSaved(WorkflowDefinitionVersion WorkflowDefinitionVersion) : INotification;