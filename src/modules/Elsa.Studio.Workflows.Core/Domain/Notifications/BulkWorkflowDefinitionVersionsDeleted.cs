using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record BulkWorkflowDefinitionVersionsDeleted(ICollection<WorkflowDefinitionVersion> Versions) : INotification;