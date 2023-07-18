using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

public partial class ActivityPicker : IDisposable,
    INotificationHandler<WorkflowDefinitionDeleted>, 
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionsBulkDeleted>,
    INotificationHandler<WorkflowDefinitionsBulkPublished>,
    INotificationHandler<WorkflowDefinitionsBulkRetracted>
{
    private string _searchText = "";
    private IEnumerable<IGrouping<string, ActivityDescriptor>> _groupedActivityDescriptors = Enumerable.Empty<IGrouping<string, ActivityDescriptor>>();

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        Mediator.Subscribe<WorkflowDefinitionPublished>(this);
        Mediator.Subscribe<WorkflowDefinitionDeleted>(this);
        await LoadActivityDescriptorsAsync();
    }

    private async Task LoadActivityDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        await ActivityRegistry.RefreshAsync(cancellationToken);
        var activities = ActivityRegistry.List();
        activities = activities.Where(x => x.IsBrowsable);
        _groupedActivityDescriptors = activities.GroupBy(x => x.Category).ToList();
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