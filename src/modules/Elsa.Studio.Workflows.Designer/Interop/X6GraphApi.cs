using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

public class X6GraphApi
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private readonly string _containerId;

    public X6GraphApi(Lazy<Task<IJSObjectReference>> moduleTask, string containerId)
    {
        _moduleTask = moduleTask;
        _containerId = containerId;
    }

    public async Task<JsonElement> ReadGraphAsync() => await InvokeAsync(module => module.InvokeAsync<JsonElement>("readGraph", _containerId));
    public async Task DisposeGraphAsync() => await InvokeAsync(module => module.InvokeVoidAsync("disposeGraph", _containerId));
    public async Task SetGridColorAsync(string color) => await InvokeAsync(module => module.InvokeVoidAsync("setGridColor", _containerId, color));
    public async Task AddActivityNodeAsync(X6Node node) => await InvokeAsync(module => module.InvokeVoidAsync("addActivityNode", _containerId, node));
    public async Task LoadGraphAsync(X6Graph graph) => await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, graph));
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _containerId));
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _containerId));

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }
    
    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }
}