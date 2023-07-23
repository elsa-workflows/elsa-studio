using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.ActivityProperties;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Viewer
{
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _propertiesPaneHeight = 600;
    private IDictionary<string, JsonObject> _activityLookup = new Dictionary<string, JsonObject>();
    private IDictionary<string, ICollection<ActivityExecutionRecord>> _activityExecutionRecordsLookup = new Dictionary<string, ICollection<ActivityExecutionRecord>>();

    [Parameter] public WorkflowInstance WorkflowInstance { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Parameter] public JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    [Parameter] public Func<DesignerPathChangedArgs, Task>? PathChanged { get; set; }
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;

    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    public string? SelectedActivityId { get; set; }
    public ICollection<ActivityExecutionRecord> SelectedActivityExecutions { get; set; } = new List<ActivityExecutionRecord>();

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
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null!)
            return;

        _activityLookup = ActivityVisitor.VisitAndMap(WorkflowDefinition.Root);
        await SelectActivityAsync(WorkflowDefinition.Root);
    }

    private async Task SelectActivityAsync(JsonObject activity)
    {
        SelectedWorkflowExecutionLogRecord = null;
        SelectedActivity = activity;
        SelectedActivityId = activity.GetId();
        ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
        SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(SelectedActivityId);

        StateHasChanged();
    }

    private async Task<ICollection<ActivityExecutionRecord>> GetActivityExecutionRecordsAsync(string activityId)
    {
        if (!_activityExecutionRecordsLookup.TryGetValue(activityId, out var records))
        {
            records = (await ActivityExecutionService.ListAsync(WorkflowInstance.Id, activityId)).ToList();
            _activityExecutionRecordsLookup[activityId] = records;
        }

        return records;
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        await SelectActivityAsync(activity);

        if (ActivitySelected != null)
            await ActivitySelected(activity);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _propertiesPaneHeight = (int)visibleHeight;
    }
}