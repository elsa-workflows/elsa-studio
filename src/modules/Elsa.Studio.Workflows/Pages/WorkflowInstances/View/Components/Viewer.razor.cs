using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Viewer : IAsyncDisposable
{
    private WorkflowInstance _workflowInstance = default!;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private DiagramDesignerWrapper _designer = default!;
    private int _propertiesPaneHeight = 300;
    private IDictionary<string, JsonObject> _activityLookup = new Dictionary<string, JsonObject>();
    private IDictionary<string, ICollection<ActivityExecutionRecord>> _activityExecutionRecordsLookup = new Dictionary<string, ICollection<ActivityExecutionRecord>>();
    private Timer? _elapsedTimer;

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
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private string? SelectedActivityId { get; set; }
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

    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null!)
            return;

        _activityLookup = ActivityVisitor.VisitAndMap(WorkflowDefinition.Root);
        await SelectActivityAsync(WorkflowDefinition.Root);

        // If the workflow instance is still running, observe it.
        if (WorkflowInstance.Status == WorkflowStatus.Running)
        {
            await ObserveWorkflowInstanceAsync();
            StartElapsedTimer();
        }
    }

    protected override void OnParametersSet()
    {
        // ReSharper disable once RedundantCheckBeforeAssignment
        if (_workflowInstance != WorkflowInstance)
            _workflowInstance = WorkflowInstance;
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
            
            if(includesSelectedActivity)
                await SelectActivityAsync(SelectedActivity!);
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

    private static ActivityStats Map(ActivityExecutionStats source)
    {
        return new ActivityStats
        {
            Blocked = source.IsBlocked,
            Completed = source.CompletedCount,
            Started = source.StartedCount,
            Uncompleted = source.UncompletedCount,
        };
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