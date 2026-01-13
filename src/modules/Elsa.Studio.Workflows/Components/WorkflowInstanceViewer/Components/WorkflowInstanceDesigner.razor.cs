using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
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
    private WorkflowInstance? _workflowInstance = null!;
    private RadzenSplitterPane _activityPropertiesPane = null!;
    private DiagramDesignerWrapper? _designer;
    private ActivityDetailsTab? _activityDetailsTab = null!;
    private ActivityExecutionsTab? _activityExecutionsTab = null!;
    private int _propertiesPaneHeight = 300;
    private readonly Dictionary<string, ICollection<ActivityExecutionRecordSummary>> _activityExecutionRecordsLookup = new();
    private readonly Dictionary<string, ActivityExecutionRecord> _lastActivityExecutionRecordLookup = new();
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

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = null!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private JsonObject? RootActivity => WorkflowDefinition?.Root;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    private IWorkflowInstanceObserver? WorkflowInstanceObserver { get; set; } = null!;
    private ICollection<ActivityExecutionRecordSummary> SelectedActivityExecutions { get; set; } = new List<ActivityExecutionRecordSummary>();
    private ActivityExecutionRecord? LastActivityExecution { get; set; }
    private Timer? _refreshTimer;

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

    private MudTabs PropertyTabs { get; set; } = null!;
    private MudTabPanel EventsTabPanel { get; set; } = null!;

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
    
    /// Selects the activity by its ID.
    public async Task SelectActivityByIdAsync(string activityId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityByActivityIdAsync(activityId);
    }

    /// <summary>
    /// Selects the activity with the specified ID, and if not found in the current container, uses the node ID to navigate to the correct container first.
    /// </summary>
    /// <param name="activityId"></param>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public async Task SelectActivityByIdAsync(string activityId, string nodeId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityByActivityIdAsync(activityId, nodeId);
    }

    /// Sets the selected journal entry.
    public async Task SelectWorkflowExecutionLogRecordAsync(JournalEntry entry)
    {
        var id = entry.Record.Id;
        var nodeId = entry.Record.NodeId;
        SelectedWorkflowExecutionLogRecord = entry;
        await SelectActivityByIdAsync(id, nodeId);
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
            StopElapsedTimer();
        }
    }

    private async Task CreateObserverAsync()
    {
        if (_workflowInstance == null || _designer == null)
            return;

        await DisposeObserverAsync();
        var container = _designer.GetCurrentContainerActivityOrRoot();
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
            _lastActivityExecutionRecordLookup.Remove(activityNodeId);
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
            _elapsedTimer = new(_ => InvokeAsync(StateHasChanged), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
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
        await StopRefreshActivityStatePeriodically();
        await InvokeAsync(async () =>
        {
            var activityNodeId = activity.GetNodeId();
            SelectedActivity = activity;
            ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
            SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(activityNodeId);
            StateHasChanged();
            _activityDetailsTab?.Refresh();

            if (_activityExecutionsTab != null)
                await _activityExecutionsTab.RefreshAsync();
        });

        if (SelectedActivityExecutions.Any())
        {
            if (LastActivityExecution != null && (!LastActivityExecution.IsFused() || LastActivityExecution.Status == ActivityStatus.Running))
                RefreshActivityStatePeriodically(LastActivityExecution.Id);
        }
    }

    private async Task<ICollection<ActivityExecutionRecordSummary>> GetActivityExecutionRecordsAsync(string activityNodeId)
    {
        if (!_activityExecutionRecordsLookup.TryGetValue(activityNodeId, out var records))
        {
            records = (await ActivityExecutionService.ListSummariesAsync(WorkflowInstance!.Id, activityNodeId)).ToList();
            _activityExecutionRecordsLookup[activityNodeId] = records;
            _lastActivityExecutionRecordLookup.Remove(activityNodeId);
        }

        if (records.Any())
        {
            var lastRecord = records.Last();
            var lastActivityExecution = _lastActivityExecutionRecordLookup.GetValueOrDefault(activityNodeId);

            if (lastActivityExecution == null || lastActivityExecution.Id != lastRecord.Id)
            {
                lastActivityExecution = await ActivityExecutionService.GetAsync(lastRecord.Id);
                _lastActivityExecutionRecordLookup[activityNodeId] = lastActivityExecution!;
            }

            LastActivityExecution = lastActivityExecution;
        }
        else
        {
            LastActivityExecution = null;
        }

        return records;
    }

    private async Task UpdatePropertiesPaneHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _propertiesPaneHeight = (int)visibleHeight - 50;
    }

    private async Task RefreshSelectedItemAsync(string activityExecutionRecordId)
    {
        if (LastActivityExecution != null)
        {
            _activityExecutionRecordsLookup.Remove(LastActivityExecution.ActivityNodeId);
            SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(LastActivityExecution.ActivityNodeId);
        }

        await InvokeAsync(() =>
        {
            StateHasChanged();
            _activityDetailsTab?.Refresh();
        });
    }

    private void RefreshActivityStatePeriodically(string activityExecutionRecordId)
    {
        async void Callback(object? _)
        {
            await RefreshSelectedItemAsync(activityExecutionRecordId);

            if (LastActivityExecution == null || (LastActivityExecution.IsFused() && LastActivityExecution.Status != ActivityStatus.Running))
                await StopRefreshActivityStatePeriodically();
            else
                _refreshTimer?.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }

        _refreshTimer = new(Callback, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
    }

    private async Task StopRefreshActivityStatePeriodically()
    {
        if (_refreshTimer != null)
        {
            await _refreshTimer.DisposeAsync();
            _refreshTimer = null;
        }
    }

    private static ActivityStats Map(ActivityExecutionStats source)
    {
        return new()
        {
            Faulted = source.IsFaulted,
            Blocked = source.IsBlocked,
            Completed = source.CompletedCount,
            Started = source.StartedCount,
            Uncompleted = source.UncompletedCount,
            Metadata = source.Metadata,
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

        if (_refreshTimer != null)
            await _refreshTimer.DisposeAsync();
    }
}