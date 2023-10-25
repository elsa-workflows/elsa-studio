using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

/// <summary>
/// Displays the workflow instance.
/// </summary>
public partial class WorkflowInstanceViewer : IAsyncDisposable
{
    private WorkflowInstance _workflowInstance = default!;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private DiagramDesignerWrapper _designer = default!;
    private int _propertiesPaneHeight = 300;
    private IDictionary<string, ActivityNode> _activityNodeLookup = new Dictionary<string, ActivityNode>();
    private readonly IDictionary<string, ICollection<ActivityExecutionRecord>> _activityExecutionRecordsLookup = new Dictionary<string, ICollection<ActivityExecutionRecord>>();
    private Timer? _elapsedTimer;
    private bool _activateEventsTabPanel = false;

    /// <summary>
    /// The workflow instance.
    /// </summary>
    [Parameter]
    public WorkflowInstance WorkflowInstance { get; set; } = default!;

    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// The selected workflow execution log record.
    /// </summary>
    [Parameter]
    public JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }

    /// <summary>
    /// The path changed callback.
    /// </summary>
    [Parameter]
    public Func<DesignerPathChangedArgs, Task>? PathChanged { get; set; }

    /// <summary>
    /// The activity selected callback.
    /// </summary>
    [Parameter]
    public Func<JsonObject, Task>? ActivitySelected { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private IWorkflowInstanceObserver WorkflowInstanceObserver { get; set; } = default!;
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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null!)
            return;

        _activityNodeLookup = await ActivityVisitor.VisitAndMapAsync(WorkflowDefinition);

        // If the workflow instance is still running, observe it.
        if (WorkflowInstance.Status == WorkflowStatus.Running)
        {
            await ObserveWorkflowInstanceAsync();
            StartElapsedTimer();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // ReSharper disable once RedundantCheckBeforeAssignment
        if (_workflowInstance != WorkflowInstance)
            _workflowInstance = WorkflowInstance;

        // If a workflow execution log record is selected, check to see if it's associated with an activity.
        if (SelectedWorkflowExecutionLogRecord != null)
        {
            var nodeId = SelectedWorkflowExecutionLogRecord.Record.NodeId;
            var activityNode = _activityNodeLookup.TryGetValue(nodeId, out var activityObject) ? activityObject : default;

            if (activityNode != null)
            {
                _activateEventsTabPanel = true;

                await SelectActivityAsync(activityNode);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await HandleActivitySelectedAsync(WorkflowDefinition!.Root);
            await UpdatePropertiesPaneHeightAsync();
        }

        if (_activateEventsTabPanel)
        {
            PropertyTabs.ActivatePanel(EventsTabPanel);
            _activateEventsTabPanel = false;
        }
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(WorkflowInstance.Id);
        WorkflowInstanceObserver.ActivityExecutionLogUpdated += async message => await InvokeAsync(async () =>
        {
            foreach (var stats in message.Stats)
            {
                var activityId = stats.ActivityId;
                _activityExecutionRecordsLookup.Remove(activityId);
                await _designer.UpdateActivityStatsAsync(activityId, Map(stats));
            }

            StateHasChanged();

            // If we received an update for the selected activity, refresh the activity details.
            var selectedActivityId = SelectedActivity?.GetId();
            var includesSelectedActivity = selectedActivityId != null && message.Stats.Any(x => x.ActivityId == selectedActivityId);

            if (includesSelectedActivity)
                await HandleActivitySelectedAsync(SelectedActivity!);
        });

        WorkflowInstanceObserver.WorkflowInstanceUpdated += async _ => await InvokeAsync(async () =>
        {
            _workflowInstance = (await WorkflowInstanceService.GetAsync(_workflowInstance.Id))!;

            if (_workflowInstance.Status == WorkflowStatus.Finished)
            {
                if (_elapsedTimer != null)
                    await _elapsedTimer.DisposeAsync();
            }
        });
    }

    private void StartElapsedTimer()
    {
        _elapsedTimer = new Timer(_ => InvokeAsync(StateHasChanged), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private async Task SelectActivityAsync(ActivityNode activityNode)
    {
        await _designer.SelectActivityAsync(activityNode);
    }

    private async Task HandleActivitySelectedAsync(JsonObject activity)
    {
        var activityNodeId = activity.GetNodeId()!;
        SelectedActivity = activity;
        ActivityDescriptor = ActivityRegistry.Find(activity!.GetTypeName(), activity!.GetVersion());
        SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(activityNodeId);

        StateHasChanged();
    }

    private async Task<ICollection<ActivityExecutionRecord>> GetActivityExecutionRecordsAsync(string activityNodeId)
    {
        if (!_activityExecutionRecordsLookup.TryGetValue(activityNodeId, out var records))
        {
            records = (await ActivityExecutionService.ListAsync(WorkflowInstance.Id, activityNodeId)).ToList();
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
        SelectedWorkflowExecutionLogRecord = null;
        await HandleActivitySelectedAsync(activity);

        if (ActivitySelected != null)
            await ActivitySelected(activity);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        await UpdatePropertiesPaneHeightAsync();
    }

    private Task OnEditClicked(string definitionId)
    {
        NavigationManager.NavigateTo($"/workflows/definitions/{definitionId}/edit");
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (WorkflowInstanceObserver != null!)
            await WorkflowInstanceObserver.DisposeAsync();

        if (_elapsedTimer != null!)
            await _elapsedTimer.DisposeAsync();
    }
}