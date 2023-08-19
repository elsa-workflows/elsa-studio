using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A Blazor component that renders a flowchart designer.
/// </summary>
public partial class FlowchartDesigner : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"container-{Guid.NewGuid():N}";
    private DotNetObjectReference<FlowchartDesigner>? _componentRef;
    private IFlowchartMapper? _flowchartMapper = default!;
    private IActivityMapper? _activityMapper = default!;
    private X6GraphApi _graphApi = default!;
    private readonly PendingActionsQueue _pendingGraphActions;
    private RateLimitedFunc<Task> _rateLimitedLoadFlowchartAction;
    private IDictionary<string, ActivityStats>? _activityStats;

    /// <inheritdoc />
    public FlowchartDesigner()
    {
        _pendingGraphActions = new PendingActionsQueue(() => new(_graphApi != null!));
        
        _rateLimitedLoadFlowchartAction = Debouncer.Debounce(async () =>
        {
            await InvokeAsync(async () => await LoadFlowchartAsync(Flowchart, ActivityStats));
        }, TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// The flowchart to render.
    /// </summary>
    [Parameter] public JsonObject Flowchart { get; set; } = default!;
    
    /// <summary>
    /// The activity stats to render.
    /// </summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    
    /// <summary>
    /// Whether the flowchart is read-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// An event raised when an activity is selected.
    /// </summary>
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }
    
    /// <summary>
    /// An event raised when an activity embedded port is selected.
    /// </summary>
    [Parameter] public Func<ActivityEmbeddedPortSelectedArgs, Task>? ActivityEmbeddedPortSelected { get; set; }
    
    /// <summary>
    /// An event raised when the canvas is selected.
    /// </summary>
    [Parameter] public Func<Task>? CanvasSelected { get; set; }
    
    /// <summary>
    /// An event raised when the graph is updated.
    /// </summary>
    [Parameter] public Func<Task>? GraphUpdated { get; set; }
    
    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = default!;

    /// <summary>
    /// Invoked from JavaScript when an activity is selected.
    /// </summary>
    /// <param name="activity">The selected activity.</param>
    [JSInvokable]
    public async Task HandleActivitySelected(JsonObject activity)
    {
        if (ActivitySelected == null)
            return;

        await InvokeAsync(async () => await ActivitySelected(activity));
    }

    /// <summary>
    /// Invoked from JavaScript when an activity embedded port is selected.
    /// </summary>
    /// <param name="activity">The selected activity.</param>
    /// <param name="portName">The selected port name.</param>
    [JSInvokable]
    public async Task HandleActivityEmbeddedPortSelected(JsonObject activity, string portName)
    {
        if (ActivityEmbeddedPortSelected == null)
            return;

        var args = new ActivityEmbeddedPortSelectedArgs(activity, portName);
        await InvokeAsync(async () => await ActivityEmbeddedPortSelected(args));
    }

    /// <summary>
    /// Invoked from JavaScript when the canvas is selected.
    /// </summary>
    [JSInvokable]
    public async Task HandleCanvasSelected()
    {
        if (CanvasSelected == null)
            return;

        await InvokeAsync(async () => await CanvasSelected());
    }

    /// <summary>
    /// Invoked from JavaScript when the graph is updated.
    /// </summary>
    [JSInvokable]
    public async Task HandleGraphUpdated()
    {
        if (GraphUpdated == null)
            return;

        await InvokeAsync(async () => await GraphUpdated());
    }

    /// <summary>
    /// Reads the flowchart from the graph.
    /// </summary>
    /// <returns>The flowchart.</returns>
    public async Task<JsonObject> ReadFlowchartAsync()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return await ScheduleGraphActionAsync(async () =>
        {
            var data = await _graphApi.ReadGraphAsync();
            var cells = data.GetProperty("cells").EnumerateArray();
            var nodes = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-activity").Select(x => x.Deserialize<X6ActivityNode>(serializerOptions)!).ToList();
            var edges = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-edge").Select(x => x.Deserialize<X6Edge>(serializerOptions)!).ToList();
            var graph = new X6Graph(nodes, edges);
            var flowchartMapper = await GetFlowchartMapperAsync();
            var exportedFlowchart = flowchartMapper.Map(graph);
            var activities = exportedFlowchart.GetActivities();
            var connections = exportedFlowchart.GetConnections();

            var flowchart = Flowchart;
            flowchart.SetProperty(activities, "activities");
            flowchart.SetProperty(connections.SerializeToNode(), "connections");

            return flowchart;
        });
    }

    /// <summary>
    /// Loads the flowchart into the graph.
    /// </summary>
    /// <param name="activity">The flowchart to load.</param>
    /// <param name="activityStats">The activity stats to load.</param>
    public async Task LoadFlowchartAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        Flowchart = activity;
        ActivityStats = activityStats;
        var flowchartMapper = await GetFlowchartMapperAsync();
        var flowchart = activity.GetFlowchart();
        var graph = flowchartMapper.Map(flowchart, activityStats);
        await ScheduleGraphActionAsync(() => _graphApi.LoadGraphAsync(graph));
    }

    /// <summary>
    /// Adds an activity to the graph.
    /// </summary>
    /// <param name="activity">The activity to add.</param>
    public async Task AddActivityAsync(JsonObject activity)
    {
        var mapper = await GetActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await ScheduleGraphActionAsync(() => _graphApi.AddActivityNodeAsync(node));
    }

    /// <summary>
    /// Zoom the canvas to fit all activities.
    /// </summary>
    public async Task ZoomToFitAsync() => await ScheduleGraphActionAsync(() => _graphApi.ZoomToFitAsync());
    /// <summary>
    /// Center the canvas content.
    /// </summary>
    public async Task CenterContentAsync() => await ScheduleGraphActionAsync(() => _graphApi.CenterContentAsync());
    
    /// <summary>
    /// Update the specified activity on the graph.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="activity">The updated activity.</param>
    public async Task UpdateActivityAsync(string id, JsonObject activity) => await ScheduleGraphActionAsync(() => _graphApi.UpdateActivityAsync(id, activity));
    
    
    /// <summary>
    /// Update the specified activity stats on the graph.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="stats">The updated activity stats.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats) => await ScheduleGraphActionAsync(() => DesignerJsInterop.UpdateActivityStatsAsync($"activity-{activityId}", activityId, stats));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ThemeService.IsDarkModeChanged += OnDarkModeChanged;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly);
            await LoadFlowchartAsync(Flowchart, ActivityStats);
            await _pendingGraphActions.ProcessAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if(!Equals(_activityStats, ActivityStats))
        {
            _activityStats = ActivityStats;
            await _rateLimitedLoadFlowchartAction.InvokeAsync();
        }
    }

    private async Task<IFlowchartMapper> GetFlowchartMapperAsync() => _flowchartMapper ??= await MapperFactory.CreateFlowchartMapperAsync();
    private async Task<IActivityMapper> GetActivityMapperAsync() => _activityMapper ??= await MapperFactory.CreateActivityMapperAsync();
    private async Task SetGridColorAsync(string color) => await ScheduleGraphActionAsync(() => _graphApi.SetGridColorAsync(color));
    private async Task ScheduleGraphActionAsync(Func<Task> action) => await _pendingGraphActions.EnqueueAsync(action);
    private async Task<T> ScheduleGraphActionAsync<T>(Func<Task<T>> action) => await _pendingGraphActions.EnqueueAsync(action);

    private async void OnDarkModeChanged()
    {
        var palette = ThemeService.CurrentPalette;
        var gridColor = palette.BackgroundGrey;
        await SetGridColorAsync(gridColor.ToString(MudColorOutputFormats.HexA));
    }
    
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_graphApi != null!)
            await _graphApi.DisposeGraphAsync();

        ((IDisposable)this).Dispose();
    }

    void IDisposable.Dispose()
    {
        ThemeService.IsDarkModeChanged -= OnDarkModeChanged;
        _componentRef?.Dispose();
    }
}