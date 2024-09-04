using System.Text.Json.Nodes;

namespace Elsa.Studio.Persistence;

/// <summary>
/// Represents a component that can persist its state.
/// </summary>
public interface IPersistentComponent
{
    /// <summary>Returns the name of the root field. For example, "Pager". </summary>
    string GetKey();

    /// <summary>Describes the default lifetime policy for this component's state.</summary>
    JsonNode GetLifetimePolicy();

    /// <summary>Retrieves the current state for persistence.</summary>
    JsonNode GetState();

    /// <summary>Applies the persisted state to the component.</summary>
    void ApplyState(JsonNode state);
}