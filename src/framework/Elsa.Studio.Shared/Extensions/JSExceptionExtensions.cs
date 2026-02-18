using Microsoft.JSInterop;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Extension methods for <see cref="JSException"/>.
/// </summary>
public static class JSExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception is a Monaco editor not found error.
    /// This occurs when a Monaco editor is being disposed while initialization is happening,
    /// causing a race condition in Blazor WASM.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is a Monaco editor not found error; otherwise, false.</returns>
    public static bool IsMonacoEditorNotFoundError(this JSException exception)
    {
        return exception.Message.Contains("Couldn't find the editor");
    }
}

/// <summary>
/// Extension methods for <see cref="ObjectDisposedException"/>.
/// </summary>
public static class ObjectDisposedExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception is a Monaco editor disposed error.
    /// This occurs when the Monaco editor's DotNetObjectReference has been disposed
    /// but operations are still trying to access it, causing a race condition in Blazor WASM.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is a Monaco editor disposed error; otherwise, false.</returns>
    public static bool IsMonacoEditorDisposedError(this ObjectDisposedException exception)
    {
        return exception.ObjectName.Contains("BlazorMonaco.Editor");
    }
}

/// <summary>
/// Extension methods for executing operations with Monaco editor error handling.
/// </summary>
public static class MonacoOperationExtensions
{
    /// <summary>
    /// Executes an asynchronous operation with automatic error handling for Monaco editor race conditions.
    /// If a Monaco editor not found error occurs, the operation is gracefully ignored.
    /// This is useful for operations that may fail due to timing issues when editors are being disposed.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A task that completes when the operation finishes or the error is caught.</returns>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (JSException ex) when (ex.IsMonacoEditorNotFoundError())
        {
            // This can happen when the component is being disposed while the Monaco editor is initializing.
            // This is a timing issue in Blazor WASM where the disposal and creation of Monaco editors can race.
            // We can safely ignore this error as the component is being recreated anyway.
        }
        catch (ObjectDisposedException ex) when (ex.IsMonacoEditorDisposedError())
        {
            // This can happen when the Monaco editor's DotNetObjectReference has been disposed
            // but operations are still trying to access it. This is a timing issue in Blazor WASM
            // where the disposal and creation of Monaco editors can race.
            // We can safely ignore this error as the component is being disposed anyway.
        }
    }

    /// <summary>
    /// Executes an asynchronous operation with automatic error handling for Monaco editor race conditions,
    /// and ensures a finally action is executed regardless of success or failure.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="finallyAction">An async action to execute in a finally block.</param>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation, Action finallyAction)
    {
        await ExecuteMonacoOperationAsync(operation, () =>
        {
            finallyAction();
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Executes an asynchronous operation with automatic error handling for Monaco editor race conditions,
    /// and ensures a finally action is executed regardless of success or failure.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="finallyAction">An async action to execute in a finally block.</param>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation, Func<Task> finallyAction)
    {
        try
        {
            await operation();
        }
        catch (JSException ex) when (ex.IsMonacoEditorNotFoundError())
        {
            // This can happen when the component is being disposed while the Monaco editor is initializing.
            // This is a timing issue in Blazor WASM where the disposal and creation of Monaco editors can race.
            // We can safely ignore this error as the component is being recreated anyway.
        }
        catch (ObjectDisposedException ex) when (ex.IsMonacoEditorDisposedError())
        {
            // This can happen when the Monaco editor's DotNetObjectReference has been disposed
            // but operations are still trying to access it. This is a timing issue in Blazor WASM
            // where the disposal and creation of Monaco editors can race.
            // We can safely ignore this error as the component is being disposed anyway.
        }
        finally
        {
            await finallyAction();
        }
    }
}

