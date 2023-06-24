using Elsa.Api.Client.Activities;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Represents a diagram editor.
/// </summary>
public interface IDiagramDesigner
{
    /// <summary>
    /// Updates the specified activity in the diagram. This is used to update the diagram when an activity is updated in the activity editor.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    Task UpdateActivityAsync(Activity activity);
    
    /// <summary>
    /// Reads the root activity from the diagram.
    /// </summary>
    Task<Activity> ReadRootActivityAsync();
    
    /// <summary>
    /// Display the designer.
    /// </summary>
    RenderFragment DisplayDesigner(DisplayContext context);
}

public record DisplayContext(Activity Activity, Func<Activity, Task> ActivitySelectedCallback, Func<Task> GraphUpdatedCallback);