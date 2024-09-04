using System.Text.Json.Nodes;

namespace Elsa.Studio.Persistence;

/// <summary>
/// Represents a component that can persist its state.
/// </summary>
public interface IPersistentComponent
{
    /// <summary>
    /// The hierarchical key for this component.
    /// </summary>
    string HierarchicalKey { get; set; }
    
    /// <summary>Describes the default lifetime policy for this component's state.</summary>
    JsonNode GetLifetimePolicy();

    /// <summary>Retrieves the current state for persistence.</summary>
    JsonNode GetState();

    /// <summary>Applies the persisted state to the component.</summary>
    void ApplyState(JsonNode state);
}