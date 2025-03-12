using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

public partial class WorkflowDefinitionVersionViewer
{
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;
    private DiagramDesignerWrapper? _diagramDesigner;

    /// Gets or sets the workflow definition to view.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    

    /// Gets or sets the event triggered when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    /// Gets the ID of the selected activity.
    public string? SelectedActivityId { get; private set; }
    
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;

    private JsonObject? Activity => _workflowDefinition?.Root;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private ActivityProperties.ActivityPropertiesPanel? ActivityPropertiesTab { get; set; }

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value, so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _workflowDefinition = WorkflowDefinition;
        await ActivityRegistry.EnsureLoadedAsync();

        if (_workflowDefinition?.Root == null)
            return;

        SelectActivity(_workflowDefinition.Root);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (WorkflowDefinition == _workflowDefinition)
            return;
     
        _workflowDefinition = WorkflowDefinition;
        
        if (_workflowDefinition?.Root == null)
            return;

        if (_diagramDesigner != null)
            await _diagramDesigner.LoadActivityAsync(_workflowDefinition.Root);

        SelectActivity(_workflowDefinition.Root);
    }

    private void SelectActivity(JsonObject activity)
    {
        // Setting the activity to null first and then requesting an update is a workaround to ensure that BlazorMonaco gets destroyed first.
        // Otherwise, the Monaco editor will not be updated with a new value. Perhaps we should consider updating the Monaco Editor via its imperative API instead of via binding.
        SelectedActivity = null;
        ActivityDescriptor = null;
        StateHasChanged();

        SelectedActivity = activity;
        SelectedActivityId = activity.GetId();
        ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
        StateHasChanged();
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        SelectActivity(activity);
        if(ActivitySelected != null)
            await ActivitySelected(activity);
    }

    private async Task OnSelectedActivityUpdated(JsonObject activity)
    {
        StateHasChanged();
        await _diagramDesigner!.UpdateActivityAsync(SelectedActivityId!, activity);
    }

    private async Task OnDownloadClicked()
    {
        var download = await WorkflowDefinitionService.ExportDefinitionAsync(_workflowDefinition!.DefinitionId, VersionOptions.SpecificVersion(_workflowDefinition.Version));
        var fileName = $"{_workflowDefinition.Name.Kebaberize()}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight + 50;
    }
}