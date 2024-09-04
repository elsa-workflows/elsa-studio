using System.Text.Json.Nodes;

namespace Elsa.Studio.Persistence;

public class StateManager : IStateManager
{
    private readonly HierarchicalGraph<IPersistentComponent> _graph = new();
    private readonly JsonObject _state = new();

    /// <inheritdoc />
    public void RegisterComponent(IPersistentComponent persistentComponent)
    {
        var parent = GetCurrentLeaf();
        _graph.AddRelationship(parent, persistentComponent);
        
        var key = GetHierarchicalKey(persistentComponent);
        var state = _state[key] ?? persistentComponent.GetState();
        persistentComponent.ApplyState(state);
    }

    /// <inheritdoc />
    public void UnregisterComponent(IPersistentComponent persistentComponent)
    {
        var state = persistentComponent.GetState();
        var key = GetHierarchicalKey(persistentComponent);
        _state[key] = state;
        var parentComponent = _graph.GetParent(persistentComponent);
        _graph.RemoveRelationship(parentComponent, persistentComponent);
    }

    /// <inheritdoc />
    public IPersistentComponent GetCurrentLeaf()
    {
        return _graph.GetAllNodes().LastOrDefault();
    }
    
    private string GetHierarchicalKey(IPersistentComponent persistentComponent)
    {
        return _graph.GetAncestors(persistentComponent).Concat([persistentComponent]).Select(x => x?.GetKey() ?? "").Aggregate((x, y) => x == "" ? y : $"{x}.{y}");
    }
}
