using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Persistence;

/// <summary>
/// Represents a component that can participate in state persistence.
/// </summary>
public abstract class PersistentComponentBase : ComponentBase, IPersistentComponent, IAsyncDisposable
{
    /// <summary>
    /// The <see cref="IStateManager"/>.
    /// </summary>
    [Inject] protected IStateManager StateManager { get; set; } = default!;

    /// <inheritdoc />
    [Parameter] public virtual string HierarchicalKey { get; set; } = default!;

    /// <inheritdoc />
    public virtual JsonNode GetLifetimePolicy() => JsonValue.Create(ComponentStateLifetime.Session.ToString());

    /// <inheritdoc />
    public virtual JsonNode GetState() => new JsonObject();

    /// <inheritdoc />
    public virtual void ApplyState(JsonNode state)
    {
    }
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await StateManager.LoadStateAsync(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StateManager.SaveStateAsync(this);
    }
}