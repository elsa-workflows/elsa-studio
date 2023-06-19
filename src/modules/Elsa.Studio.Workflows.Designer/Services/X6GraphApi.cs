using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Services;

public class X6GraphApi
{
    private readonly IJSObjectReference _module;
    private readonly string _containerId;

    public X6GraphApi(IJSObjectReference module, string containerId)
    {
        _module = module;
        _containerId = containerId;
    }
    
    public async Task<JsonElement> ReadGraphAsync() => await InvokeAsync(module => module.InvokeAsync<JsonElement>("readGraph", _containerId));
    public async Task DisposeGraphAsync() => await InvokeAsync(module => module.InvokeVoidAsync("disposeGraph", _containerId));
    public async Task SetGridColorAsync(string color) => await InvokeAsync(module => module.InvokeVoidAsync("setGridColor", _containerId, color));
    public async Task AddActivityNodeAsync(X6Node node) => await InvokeAsync(module => module.InvokeVoidAsync("addActivityNode", _containerId, node));
    public async Task LoadGraphAsync(X6Graph graph) => await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, graph));
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _containerId));
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _containerId));
    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func) => await func(_module);
    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func) => await func(_module);
}