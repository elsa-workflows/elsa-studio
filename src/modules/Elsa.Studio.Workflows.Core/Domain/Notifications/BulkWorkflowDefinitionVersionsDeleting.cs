using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionVersionsDeleting(ICollection<WorkflowDefinitionVersion> Versions) : INotification;