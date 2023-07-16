using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Contexts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Represents a diagram editor.
/// </summary>
public interface IDiagramDesigner
{
    bool IsInitialized { get; }
    
    /// <summary>
    /// Loads the specified root activity int the designer.
    /// </summary>
    /// <param name="activity">The root activity to load.</param>
    Task LoadRootActivity(JsonObject activity);
    
    /// <summary>
    /// Updates the specified activity in the diagram. This is used to update the diagram when an activity is updated in the activity editor.
    /// </summary>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    Task UpdateActivityAsync(string id, JsonObject activity);
    
    /// <summary>
    /// Reads the root activity from the diagram.
    /// </summary>
    Task<JsonObject> ReadRootActivityAsync();
    
    /// <summary>
    /// Display the designer.
    /// </summary>
    RenderFragment DisplayDesigner(DisplayContext context);
}