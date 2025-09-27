using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides a base class for JavaScript interop helpers that lazily load and dispose ES modules.
/// </summary>
public abstract class JsInteropBase : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    /// <summary>
    /// Gets the file name of the JavaScript module to load.
    /// </summary>
    protected abstract string ModuleName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsInteropBase"/> class using the specified runtime.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime used to import the module.</param>
    protected JsInteropBase(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/Elsa.Studio.DomInterop/{ModuleName}.entry.js").AsTask());
    }

    /// <summary>
    /// Disposes the imported JavaScript module when it has been created.
    /// </summary>
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

    /// <summary>
    /// Invokes the provided asynchronous delegate with the imported module, creating it if necessary.
    /// </summary>
    /// <param name="func">The callback to execute against the module.</param>
    /// <returns>A task that completes when the invocation finishes.</returns>
    protected async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }

    /// <summary>
    /// Invokes the provided asynchronous delegate with the imported module and returns its result.
    /// </summary>
    /// <typeparam name="T">The type of value returned by the delegate.</typeparam>
    /// <param name="func">The callback to execute against the module.</param>
    /// <returns>The value returned by <paramref name="func"/>.</returns>
    protected async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }
}