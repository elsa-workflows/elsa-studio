using System.Drawing;
using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
internal class DesignerJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public DesignerJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Elsa.Studio.Workflows.Designer/designer.entry.js").AsTask());
    }

    /// <summary>
    /// Creates a new X6 graph object and returns its ID.
    /// </summary>
    /// <param name="containerId">The ID of the container element.</param>
    /// <returns>The ID of the graph.</returns>
    public async ValueTask<string> CreateGraphAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("createGraph", containerId);
    }
    
    public async Task DisposeGraphAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("disposeGraph", containerId);
    }

    public async ValueTask SetGridColorAsync(string containerId, string color)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setGridColor", containerId, color);
    }
    
    public async ValueTask UpdateActivityNodeAsync(string elementId, JsonElement activity)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateActivityNode", elementId, activity);
    }
    
    public async ValueTask AddActivityNodeAsync(string containerId, X6Node node)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("addActivityNode", containerId, node);
    }

    public async ValueTask<object> LoadDataAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<object>("loadData", containerId);
    }

    public async ValueTask LoadGraphAsync(string containerId, X6Graph graph)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("loadGraph", containerId, graph);
    }

    public async Task ZoomToFitAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("zoomToFit", containerId);
    }
    
    public async Task CenterContentAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("centerContent", containerId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;

            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // This exception is thrown when the browser window is closed or the page reloads.
                // We can safely ignore it.
            }
        }
    }
}