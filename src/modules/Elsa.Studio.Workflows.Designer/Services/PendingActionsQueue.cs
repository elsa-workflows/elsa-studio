namespace Elsa.Studio.Workflows.Designer.Services;

public class PendingActionsQueue
{
    private readonly Queue<Func<Task>> _pendingActions = new();
    private readonly Func<ValueTask<bool>> _shortCircuit;

    public PendingActionsQueue(Func<ValueTask<bool>> shortCircuit)
    {
        _shortCircuit = shortCircuit;
    }
    
    public async Task ProcessAsync()
    {
        while (_pendingActions.Any())
        {
            var action = _pendingActions.Dequeue();
            await action();
        }
    }

    public async Task EnqueueAsync(Func<Task> action)
    {
        if(await _shortCircuit())
        {
            await action();
            return;
        }

        var tsc = new TaskCompletionSource();
        _pendingActions.Enqueue(async () =>
        {
            await action();
            tsc.SetResult();
        });
        await tsc.Task;
    }

    public async Task<T> EnqueueAsync<T>(Func<Task<T>> action)
    {
        if(await _shortCircuit())
            return await action();
        
        var tsc = new TaskCompletionSource<T>();
        _pendingActions.Enqueue(async () =>
        {
            var result = await action();
            tsc.SetResult(result);
        });
        return await tsc.Task;
    }
}