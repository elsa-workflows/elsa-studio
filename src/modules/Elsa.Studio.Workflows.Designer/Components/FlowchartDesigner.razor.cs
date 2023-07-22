using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

public partial class FlowchartDesigner : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"container-{Guid.NewGuid():N}";
    private DotNetObjectReference<FlowchartDesigner>? _componentRef;
    private IFlowchartMapper? _flowchartMapper = default!;
    private IActivityMapper? _activityMapper = default!;
    private X6GraphApi _graphApi = default!;
    private readonly PendingActionsQueue _pendingGraphActions;
    private RateLimitedFunc<Task> _rateLimitedLoadFlowchartAction;

    public FlowchartDesigner()
    {
        _pendingGraphActions = new PendingActionsQueue(() => new(_graphApi != null!));
        
        _rateLimitedLoadFlowchartAction = Debouncer.Debounce(async () =>
        {
            await InvokeAsync(async () => await LoadFlowchartAsync(Flowchart, ActivityStats));
        }, TimeSpan.FromMilliseconds(100));
    }

    [Parameter] public JsonObject Flowchart { get; set; } = default!;
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }
    [Parameter] public Func<ActivityEmbeddedPortSelectedArgs, Task>? ActivityEmbeddedPortSelected { get; set; }
    [Parameter] public Func<Task>? CanvasSelected { get; set; }
    [Parameter] public Func<Task>? GraphUpdated { get; set; }
    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = default!;

    [JSInvokable]
    public async Task HandleActivitySelected(JsonObject activity)
    {
        if (ActivitySelected == null)
            return;

        await InvokeAsync(async () => await ActivitySelected(activity));
    }

    [JSInvokable]
    public async Task HandleActivityEmbeddedPortSelected(JsonObject activity, string portName)
    {
        if (ActivityEmbeddedPortSelected == null)
            return;

        var args = new ActivityEmbeddedPortSelectedArgs(activity, portName);
        await InvokeAsync(async () => await ActivityEmbeddedPortSelected(args));
    }

    [JSInvokable]
    public async Task HandleCanvasSelected()
    {
        if (CanvasSelected == null)
            return;

        await InvokeAsync(async () => await CanvasSelected());
    }

    [JSInvokable]
    public async Task HandleGraphUpdated()
    {
        if (GraphUpdated == null)
            return;

        await InvokeAsync(async () => await GraphUpdated());
    }

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

    public async Task LoadFlowchartAsync(JsonObject flowchart, IDictionary<string, ActivityStats>? activityStats)
    {
        Flowchart = flowchart;
        ActivityStats = activityStats;
        var flowchartMapper = await GetFlowchartMapperAsync();
        var graph = flowchartMapper.Map(flowchart, activityStats);
        await ScheduleGraphActionAsync(() => _graphApi.LoadGraphAsync(graph));
    }

    public async Task AddActivityAsync(JsonObject activity)
    {
        var mapper = await GetActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await ScheduleGraphActionAsync(() => _graphApi.AddActivityNodeAsync(node));
    }

    public async Task ZoomToFitAsync() => await ScheduleGraphActionAsync(() => _graphApi.ZoomToFitAsync());
    public async Task CenterContentAsync() => await ScheduleGraphActionAsync(() => _graphApi.CenterContentAsync());
    public async Task UpdateActivityAsync(string id, JsonObject activity) => await ScheduleGraphActionAsync(() => _graphApi.UpdateActivityAsync(id, activity));

    public async ValueTask DisposeAsync()
    {
        if (_graphApi != null!)
            await _graphApi.DisposeGraphAsync();

        Dispose();
    }

    public void Dispose()
    {
        ThemeService.IsDarkModeChanged -= OnDarkModeChanged;
        _componentRef?.Dispose();
    }

    protected override void OnInitialized()
    {
        ThemeService.IsDarkModeChanged += OnDarkModeChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly);
            await _pendingGraphActions.ProcessAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await _rateLimitedLoadFlowchartAction.InvokeAsync();
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
}