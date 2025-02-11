using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionExporting(WorkflowDefinition WorkflowDefinition) : INotification;