using Microsoft.JSInterop;

namespace Elsa.Studio.WorkflowDesigner.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public class DesignerJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public DesignerJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Elsa.Studio.WorkflowDesigner/designer.entry.js").AsTask());
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

    public async ValueTask SetGridColorAsync(string graphId, string color)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setGridColor", graphId, color);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}