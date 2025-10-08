using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Elsa.Studio.Workflows.Handlers;

/// A handler that refreshes the activity registry when a workflow definition is deleted, published or retracted.
#if JETBRAINS_ANNOTATIONS
[UsedImplicitly]
#endif
public class RefreshActivityRegistry : INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<BulkWorkflowDefinitionsDeleted>,
    INotificationHandler<BulkWorkflowDefinitionVersionsDeleted>,
    INotificationHandler<BulkWorkflowDefinitionsPublished>,
    INotificationHandler<BulkWorkflowDefinitionsRetracted>
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
    async Task INotificationHandler<BulkWorkflowDefinitionsDeleted>.HandleAsync(BulkWorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionVersionsDeleted>.HandleAsync(BulkWorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionsPublished>.HandleAsync(BulkWorkflowDefinitionsPublished notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionsRetracted>.HandleAsync(BulkWorkflowDefinitionsRetracted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);

    private async Task RefreshActivityRegistryAsync(CancellationToken cancellationToken = default)
    {
        await _activityRegistry.RefreshAsync(cancellationToken);
    }
}