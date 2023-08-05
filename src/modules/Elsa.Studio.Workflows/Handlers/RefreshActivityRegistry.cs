using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;

namespace Elsa.Studio.Workflows.Handlers;

/// <summary>
/// A handler that refreshes the activity registry when a workflow definition is deleted, published or retracted.
/// </summary>
public class RefreshActivityRegistry :INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionsBulkDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsBulkDeleted>,
    INotificationHandler<WorkflowDefinitionsBulkPublished>,
    INotificationHandler<WorkflowDefinitionsBulkRetracted>
{
    private readonly IActivityRegistry _activityRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshActivityRegistry"/> class.
    /// </summary>
    public RefreshActivityRegistry(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }
    
    async Task INotificationHandler<WorkflowDefinitionDeleted>.HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionPublished>.HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionRetracted>.HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionsBulkDeleted>.HandleAsync(WorkflowDefinitionsBulkDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionVersionsBulkDeleted>.HandleAsync(WorkflowDefinitionVersionsBulkDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionsBulkPublished>.HandleAsync(WorkflowDefinitionsBulkPublished notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionsBulkRetracted>.HandleAsync(WorkflowDefinitionsBulkRetracted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);

    private async Task RefreshActivityRegistryAsync(CancellationToken cancellationToken = default)
    {
        await _activityRegistry.RefreshAsync(cancellationToken);
    }
}