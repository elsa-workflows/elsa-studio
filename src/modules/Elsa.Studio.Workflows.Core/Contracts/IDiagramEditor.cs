using Elsa.Api.Client.Activities;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Represents a diagram editor.
/// </summary>
public interface IDiagramEditor : IComponent
{
    /// <summary>
    /// Invoked when the user requests to save the workflow.
    /// </summary>
    EventCallback OnSaveRequested { get; set; }
    
    /// <summary>
    /// Updates the specified activity in the diagram.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    Task UpdateActivityAsync(Activity activity);
    
    /// <summary>
    /// Reads the root activity from the diagram.
    /// </summary>
    Task<Activity> ReadRootActivityAsync();
}