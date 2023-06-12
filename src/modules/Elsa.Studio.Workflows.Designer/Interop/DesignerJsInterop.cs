using System.Text.Json;
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
    public async ValueTask<X6GraphApi> CreateGraphAsync(string containerId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeAsync<string>("createGraph", containerId);
        return new X6GraphApi(_moduleTask, containerId);
    }

    public async Task UpdateActivityNodeAsync(string elementId, JsonElement activity) =>
        await InvokeAsync(module => module.InvokeVoidAsync("updateActivityNode", elementId, activity));

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

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }
}