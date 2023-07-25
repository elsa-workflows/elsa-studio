using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionVersionsBulkDeleted(ICollection<string> Ids) : INotification;