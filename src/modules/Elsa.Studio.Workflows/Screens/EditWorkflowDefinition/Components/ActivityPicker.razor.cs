using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components;

public partial class ActivityPicker : IDisposable,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionsBulkDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsBulkDeleted>,
    INotificationHandler<WorkflowDefinitionsBulkPublished>,
    INotificationHandler<WorkflowDefinitionsBulkRetracted>
{
    private string _searchText = "";

    private IEnumerable<IGrouping<string, ActivityDescriptor>> _groupedActivityDescriptors
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return ActivityDescriptors.GroupBy(x => x.Category); // Return all items grouped by category
            }
            
            var items = ActivityDescriptors.Where(item =>
                item.Name.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase));
            return items
                .GroupBy(x => x.Category); 
        }
    }

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IEnumerable<ActivityDescriptor> ActivityDescriptors = new List<ActivityDescriptor>();

    protected override async Task OnInitializedAsync()
    {
        Mediator.Subscribe<WorkflowDefinitionPublished>(this);
        Mediator.Subscribe<WorkflowDefinitionDeleted>(this);
        await LoadActivityDescriptorsAsync();
    }

    private async Task<IEnumerable<ActivityDescriptor>> GetActivityDescriptors(CancellationToken cancellationToken = default)
    {
        await ActivityRegistry.RefreshAsync(cancellationToken);
        var activities = ActivityRegistry.List();
        return activities.Where(x => x.IsBrowsable);
    }
    private async Task LoadActivityDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        ActivityDescriptors = await  GetActivityDescriptors(cancellationToken);
        StateHasChanged();
    }

    private void OnDragStart(ActivityDescriptor activityDescriptor)
    {
        DragDropManager.Payload = activityDescriptor;
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
        await ActivityRegistry.RefreshAsync(cancellationToken);
        await LoadActivityDescriptorsAsync(cancellationToken);
    }

    void IDisposable.Dispose()
    {
        Mediator.Unsubscribe(this);
    }
}