using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public abstract class JsInteropBase : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    protected abstract string ModuleName { get; }

    protected JsInteropBase(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => ImportModule(jsRuntime));
    }

    public virtual Task<IJSObjectReference> ImportModule(IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/Elsa.Studio.DomInterop/{ModuleName}.entry.js").AsTask();
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

    protected async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }

    protected async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }
}