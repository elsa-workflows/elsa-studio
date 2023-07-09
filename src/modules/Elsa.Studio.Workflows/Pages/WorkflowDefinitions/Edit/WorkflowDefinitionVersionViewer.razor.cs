using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

public partial class WorkflowDefinitionVersionViewer
{
    private IDiagramDesigner? _diagramDesigner;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;
    
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;

    private Activity? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    public string? SelectedActivityId { get; set; }
    private Pages.WorkflowDefinitions.Edit.ActivityProperties.ActivityProperties? ActivityPropertiesTab { get; set; }

    public RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }
    
    protected override async Task OnInitializedAsync()
    {
        if (WorkflowDefinition?.Root == null)
            return;
    
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(WorkflowDefinition.Root);
        await SelectActivity(WorkflowDefinition.Root);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (WorkflowDefinition?.Root == null)
            return;

        if(_diagramDesigner?.IsInitialized == false)
            return;
        
        await _diagramDesigner!.LoadRootActivity(WorkflowDefinition.Root);
        await SelectActivity(WorkflowDefinition.Root);
    }

    private async Task SelectActivity(Activity activity)
    {
        SelectedActivity = activity;
        SelectedActivityId = activity.Id;
        ActivityDescriptor = await ActivityRegistry.FindAsync(activity.Type);
        StateHasChanged();
    }
    
    private async Task OnActivitySelected(Activity activity)
    {
        await SelectActivity(activity);
    }
    
    private async Task OnDownloadClicked()
    {
        var download = await WorkflowDefinitionService.ExportDefinitionAsync(WorkflowDefinition!.DefinitionId, VersionOptions.SpecificVersion(WorkflowDefinition.Version));
        var fileName = $"{WorkflowDefinition.Name.Kebaberize()}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }
    
    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight;
    }
}