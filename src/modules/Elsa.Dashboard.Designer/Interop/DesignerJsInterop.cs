using Microsoft.JSInterop;

namespace Elsa.Dashboard.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public class DesignerJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public DesignerJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Elsa.Dashboard.Designer/designer.entry.js").AsTask());
    }

    public async ValueTask<string> CreateGraph(string containerId)
    {
        var module = await _moduleTask.Value;
         return await module.InvokeAsync<string>("createGraph", containerId);
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