using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition is about to be saved.
public record WorkflowDefinitionSaving(WorkflowDefinitionVersion WorkflowDefinitionVersion) : INotification;