namespace Elsa.Studio.Extensions;

/// <summary>
/// Extension methods for executing Monaco editor operations.
/// </summary>
public static class MonacoOperationExtensions
{
    /// <summary>
    /// Executes an asynchronous operation.
    /// This wrapper is maintained for backward compatibility but simply executes the operation directly.
    /// The Task.Yield() in InputsTab.OnParametersSetAsync prevents race conditions, making exception handling unnecessary.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation)
    {
        await operation();
    }

    /// <summary>
    /// Executes an asynchronous operation with a finally action.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="finallyAction">An action to execute in a finally block.</param>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation, Action finallyAction)
    {
        try
        {
            await operation();
        }
        finally
        {
            finallyAction();
        }
    }

    /// <summary>
    /// Executes an asynchronous operation with an async finally action.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="finallyAction">An async action to execute in a finally block.</param>
    public static async Task ExecuteMonacoOperationAsync(Func<Task> operation, Func<Task> finallyAction)
    {
        try
        {
            await operation();
        }
        finally
        {
            await finallyAction();
        }
    }
}

