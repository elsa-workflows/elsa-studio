using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public abstract class JsInteropBase : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    protected abstract string ModuleName { get; }

    /// Initializes a new instance of <see cref="JsInteropBase"/>
    protected JsInteropBase(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/Elsa.Studio.Workflows.Designer/{ModuleName}.entry.js").AsTask());
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

    protected async Task TryInvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        try
        {
            var module = await _moduleTask.Value;
            await func(module);
        }
        catch (JSException)
        {
            // Ignore.
        }
    }

    protected async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }

    protected async Task<T> TryInvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await func(module);
        }
        catch (JSException)
        {
            // Ignore.
        }
        catch(ObjectDisposedException)
        {
            // Ignore.
        }

        return default!;
    }
}