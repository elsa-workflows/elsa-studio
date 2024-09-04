using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Persistence;

/// <summary>
/// Represents a component that can participate in state persistence.
/// </summary>
public abstract class PersistentComponentBase : ComponentBase, IPersistentComponent, IDisposable
{
    /// <summary>
    /// The <see cref="IStateManager"/>.
    /// </summary>
    [Inject] protected IStateManager StateManager { get; set; } = default!;

    /// <inheritdoc />
    public virtual string GetKey() => GetType().Name;

    /// <inheritdoc />
    public virtual JsonNode GetLifetimePolicy() => new JsonObject { [GetKey()] = ComponentStateLifetime.Session.ToString() };

    /// <inheritdoc />
    public virtual JsonNode GetState() => new JsonObject { [GetKey()] = new JsonObject() };

    /// <inheritdoc />
    public virtual void ApplyState(JsonNode state)
    {
    }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        StateManager.RegisterComponent(this);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        StateManager.UnregisterComponent(this);
    }
}