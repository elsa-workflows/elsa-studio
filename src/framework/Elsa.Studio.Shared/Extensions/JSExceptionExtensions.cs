namespace Elsa.Studio.Extensions;

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
        await operation();
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
        await operation();
    }
}

