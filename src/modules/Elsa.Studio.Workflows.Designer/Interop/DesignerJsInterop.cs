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
    
    public async Task DisposeGraphAsync(string graphId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("disposeGraph", graphId);
    }

    public async ValueTask SetGridColorAsync(string graphId, string color)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setGridColor", graphId, color);
    }
    
    public async ValueTask UpdateActivityNodeAsync(string elementId, string activityId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateActivityNode", elementId, activityId);
    }

    public async ValueTask<object> LoadDataAsync(string graphId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<object>("loadData", graphId);
    }

    public async ValueTask LoadGraphAsync(string graphId, X6Graph graph)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("loadGraph", graphId, graph);
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