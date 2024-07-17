using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays the workflow instance.
/// </summary>
public partial class WorkflowInstanceDesigner : IAsyncDisposable
{
    private WorkflowInstance? _workflowInstance = default!;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private DiagramDesignerWrapper? _designer;
    private ActivityDetailsTab? _activityDetailsTab = default!;
    private ActivityExecutionsTab? _activityExecutionsTab = default!;
    private int _propertiesPaneHeight = 300;
    private readonly IDictionary<string, ICollection<ActivityExecutionRecord>> _activityExecutionRecordsLookup = new Dictionary<string, ICollection<ActivityExecutionRecord>>();
    private Timer? _elapsedTimer;

    /// The workflow instance.
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }

    /// The workflow definition.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// The path changed callback.
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }

    /// The activity selected callback.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event that is invoked when the workflow definition is requested to be edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    /// Gets or sets the current selected sub-workflow.
    [Parameter] public JsonObject? SelectedSubWorkflow { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private JsonObject? RootActivity => WorkflowDefinition?.Root;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    private IWorkflowInstanceObserver? WorkflowInstanceObserver { get; set; } = default!;
    private ICollection<ActivityExecutionRecord> SelectedActivityExecutions { get; set; } = new List<ActivityExecutionRecord>();

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    private MudTabs PropertyTabs { get; set; } = default!;
    private MudTabPanel EventsTabPanel { get; set; } = default!;

    /// Updates the selected sub-workflow.
    public void UpdateSubWorkflow(JsonObject? obj)
    {
        SelectedSubWorkflow = obj;
        StateHasChanged();
    }

    /// Selects the activity by its node ID.
    public async Task SelectActivityAsync(string nodeId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityAsync(nodeId);
    }

    /// Sets the selected journal entry.
    public async Task SelectWorkflowExecutionLogRecordAsync(JournalEntry entry)
    {
        var nodeId = entry.Record.NodeId;
        SelectedWorkflowExecutionLogRecord = entry;
        await SelectActivityAsync(nodeId);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null!)
            return;

        await UpdateObserverAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var hasDifferentState = _workflowInstance?.Id != WorkflowInstance?.Id || _workflowInstance?.Status != WorkflowInstance?.Status;

        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance!;

            if (hasDifferentState)
                await UpdateObserverAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (WorkflowDefinition != null)
                await HandleActivitySelectedAsync(WorkflowDefinition!.Root);
            await UpdatePropertiesPaneHeightAsync();
            await UpdateObserverAsync();
        }
    }

    private async Task UpdateObserverAsync()
    {
        if (WorkflowInstance?.Status == WorkflowStatus.Running)
        {
            await CreateObserverAsync();
            StartElapsedTimer();
        }
        else
        {
            await DisposeObserverAsync();
            StopElapsedTimer();
        }
    }

    private async Task CreateObserverAsync()
    {
        if (_workflowInstance == null || _designer == null)
            return;

        await DisposeObserverAsync();
        var container = _designer.GetCurrentContainerActivity();
        var observerContext = new WorkflowInstanceObserverContext
        {
            WorkflowInstanceId = _workflowInstance.Id,
            ContainerActivity = container,
        };
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(observerContext);
        WorkflowInstanceObserver.ActivityExecutionLogUpdated += OnActivityExecutionLogUpdated;
    }

    private async Task DisposeObserverAsync()
    {
        if (WorkflowInstanceObserver != null!)
        {
            WorkflowInstanceObserver.ActivityExecutionLogUpdated -= OnActivityExecutionLogUpdated;
            await WorkflowInstanceObserver.DisposeAsync();
            WorkflowInstanceObserver = null;
        }
    }

    private async Task OnActivityExecutionLogUpdated(ActivityExecutionLogUpdatedMessage message)
    {
        if (_designer == null) return;
        
        foreach (var stats in message.Stats)
        {
            var activityNodeId = stats.ActivityNodeId;
            var activityId = stats.ActivityId;
            _activityExecutionRecordsLookup.Remove(activityNodeId);
            await _designer.UpdateActivityStatsAsync(activityId, Map(stats));
        }

        await InvokeAsync(StateHasChanged);

        // If we received an update for the selected activity, refresh the activity details.
        var selectedActivityNodeId = SelectedActivity?.GetNodeId();
        var includesSelectedActivity = selectedActivityNodeId != null && message.Stats.Any(x => x.ActivityNodeId == selectedActivityNodeId);

        if (includesSelectedActivity)
        {
            await HandleActivitySelectedAsync(SelectedActivity!);
        }
    }

    private void StartElapsedTimer()
    {
        if (_elapsedTimer == null)
            _elapsedTimer = new Timer(_ => InvokeAsync(StateHasChanged), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StopElapsedTimer()
    {
        if (_elapsedTimer != null)
        {
            _elapsedTimer?.Dispose();
            _elapsedTimer = null;
        }
    }

    private async Task HandleActivitySelectedAsync(JsonObject activity)
    {
        await InvokeAsync(async () =>
        {
            var activityNodeId = activity.GetNodeId();
            SelectedActivity = activity;
            ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
            SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(activityNodeId);
            StateHasChanged();
            _activityDetailsTab?.Refresh();
            _activityExecutionsTab?.Refresh();
        });
    }

    private async Task<ICollection<ActivityExecutionRecord>> GetActivityExecutionRecordsAsync(string activityNodeId)
    {
        if (!_activityExecutionRecordsLookup.TryGetValue(activityNodeId, out var records))
        {
            records = (await InvokeWithBlazorServiceContext(() => ActivityExecutionService.ListAsync(WorkflowInstance.Id, activityNodeId))).ToList();
            _activityExecutionRecordsLookup[activityNodeId] = records;
        }

        return records;
    }

    private async Task UpdatePropertiesPaneHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _propertiesPaneHeight = (int)visibleHeight - 50;
    }

    private static ActivityStats Map(ActivityExecutionStats source)
    {
        return new ActivityStats
        {
            Faulted = source.IsFaulted,
            Blocked = source.IsBlocked,
            Completed = source.CompletedCount,
            Started = source.StartedCount,
            Uncompleted = source.UncompletedCount,
        };
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        await HandleActivitySelectedAsync(activity);

        var activitySelected = ActivitySelected;

        if (activitySelected.HasDelegate)
            await activitySelected.InvokeAsync(activity);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        await UpdatePropertiesPaneHeightAsync();
    }

    private Task OnEditClicked()
    {
        var definitionId = WorkflowDefinition!.DefinitionId;

        if (SelectedSubWorkflow != null)
        {
            var typeName = SelectedSubWorkflow.GetTypeName();
            var version = SelectedSubWorkflow.GetVersion();
            var descriptor = ActivityRegistry.Find(typeName, version);
            var isWorkflowActivity = descriptor != null &&
                                     descriptor.CustomProperties.TryGetValue("RootType", out var rootTypeNameElement) &&
                                     ((JsonElement)rootTypeNameElement).GetString() == "WorkflowDefinitionActivity";
            if (isWorkflowActivity)
            {
                definitionId = SelectedSubWorkflow.GetWorkflowDefinitionId();
            }
        }

        var editWorkflowDefinition = EditWorkflowDefinition;

        if (editWorkflowDefinition.HasDelegate)
            return editWorkflowDefinition.InvokeAsync(definitionId);

        NavigationManager.NavigateTo($"workflows/definitions/{definitionId}/edit");
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        StopElapsedTimer();
        await DisposeObserverAsync();
    }
}