using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a Flowchart activity to an X6Graph.
/// </summary>
public interface IFlowchartMapper
{
    /// <summary>
    /// Maps a Flowchart activity to an X6Graph.
    /// </summary>
    /// <param name="flowchartElement">The Flowchart activity.</param>
    /// <returns>An X6Graph.</returns>
    X6Graph MapFlowchart(JsonElement flowchartElement);
    
    /// <summary>
    /// Maps an activity to an X6Node.
    /// </summary>
    /// <param name="activityElement">The activity.</param>
    /// <returns>An X6Node.</returns>
    X6Node MapActivity(JsonElement activityElement);
    
}