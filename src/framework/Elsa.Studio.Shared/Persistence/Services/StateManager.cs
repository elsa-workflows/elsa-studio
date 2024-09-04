using System.Text.Json.Nodes;

namespace Elsa.Studio.Persistence;

public class StateManager : IStateManager
{
    private readonly JsonObject _state = new();
    /// <inheritdoc />
    public Task LoadStateAsync(IPersistentComponent persistentComponent)
    {
        var key = persistentComponent.HierarchicalKey;
        var state = _state[key] ?? persistentComponent.GetState();
        persistentComponent.ApplyState(state);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveStateAsync(IPersistentComponent persistentComponent)
    {
        var state = persistentComponent.GetState();
        var key = persistentComponent.HierarchicalKey;
        _state[key] = state;
        return Task.CompletedTask;
    }
}