using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Activities;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Elsa.Studio.Workflows.Designer.Components;

public partial class FlowchartDesigner : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"container-{Guid.NewGuid():N}";
    private DotNetObjectReference<FlowchartDesigner>? _componentRef;
    private IFlowchartMapper? _flowchartMapper = default!;
    private IActivityMapper? _activityMapper = default!;
    private X6GraphApi _graphApi = default!;

    private List<BreadcrumbItem> _activityPath = new()
    {
        new("Flowchart1", href: "#", icon: ActivityIcons.Flowchart),
        new("ForEach1", href: "#", icon: @Icons.Material.Outlined.RepeatOne),
    };

    [Parameter] public Flowchart Flowchart { get; set; } = default!;
    [Parameter] public Func<Activity, Task>? OnActivitySelected { get; set; }
    [Parameter] public Func<Task>? OnCanvasSelected { get; set; }
    [Parameter] public Func<Task>? OnGraphUpdated { get; set; }
    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = default!;

    [JSInvokable]
    public async Task HandleActivitySelected(Activity activity)
    {
        if (OnActivitySelected == null)
            return;

        await InvokeAsync(async () => await OnActivitySelected(activity));
    }

    [JSInvokable]
    public async Task HandleCanvasSelected()
    {
        if (OnCanvasSelected == null)
            return;

        await InvokeAsync(async () => await OnCanvasSelected());
    }

    [JSInvokable]
    public async Task HandleGraphUpdated()
    {
        if (OnGraphUpdated == null)
            return;

        await InvokeAsync(async () => await OnGraphUpdated());
    }

    public async Task<Flowchart> ReadFlowchartAsync()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var data = await _graphApi.ReadGraphAsync();
        var cells = data.GetProperty("cells").EnumerateArray();
        var nodes = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-activity").Select(x => x.Deserialize<X6Node>(serializerOptions)!).ToList();
        var edges = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-edge").Select(x => x.Deserialize<X6Edge>(serializerOptions)!).ToList();
        var graph = new X6Graph(nodes, edges);
        var flowchartMapper = await GetFlowchartMapperAsync();
        var exportedFlowchart = flowchartMapper.Map(graph);
        var activities = exportedFlowchart.Activities;
        var connections = exportedFlowchart.Connections;

        var flowchart = Flowchart;
        flowchart.Activities = activities;
        flowchart.Connections = connections;

        return flowchart;
    }

    public async Task LoadFlowchartAsync(Flowchart flowchart)
    {
        var flowchartMapper = await GetFlowchartMapperAsync();
        var graph = flowchartMapper.Map(flowchart);
        await _graphApi.LoadGraphAsync(graph);
    }

    public async Task AddActivityAsync(Activity activity)
    {
        var mapper = await GetActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await _graphApi.AddActivityNodeAsync(node);
    }

    public async Task ZoomToFitAsync() => await _graphApi.ZoomToFitAsync();
    public async Task CenterContentAsync() => await _graphApi.CenterContentAsync();
    public async Task UpdateActivityAsync(string id, Activity activity) => await _graphApi.UpdateActivityAsync(id, activity);

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
            _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef);

            await LoadFlowchartAsync(Flowchart);
        }
    }

    private async Task<IFlowchartMapper> GetFlowchartMapperAsync() => _flowchartMapper ??= await MapperFactory.CreateFlowchartMapperAsync();
    private async Task<IActivityMapper> GetActivityMapperAsync() => _activityMapper ??= await MapperFactory.CreateActivityMapperAsync();

    /// <summary>
    /// Sets the grid color.
    /// </summary>
    private async Task SetGridColorAsync(string color) => await _graphApi.SetGridColorAsync(color);

    private async void OnDarkModeChanged()
    {
        var palette = ThemeService.CurrentPalette;
        var gridColor = palette.BackgroundGrey;
        await SetGridColorAsync(gridColor.ToString(MudColorOutputFormats.HexA));
    }
}