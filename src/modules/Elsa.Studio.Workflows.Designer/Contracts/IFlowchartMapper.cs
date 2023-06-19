using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a Flowchart activity to an X6Graph.
/// </summary>
public interface IFlowchartMapper
{
    /// <summary>
    /// Maps a flowchart activity to an X6Graph.
    /// </summary>
    /// <param name="flowchart">The flowchart activity.</param>
    /// <returns>An X6Graph.</returns>
    X6Graph MapFlowchart(Flowchart flowchart);
    
    /// <summary>
    /// Maps an activity to an X6Node.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>An X6Node.</returns>
    X6Node MapActivity(Activity activity);
    
    /// <summary>
    /// Maps an X6 graph to a flowchart activity.
    /// </summary>
    /// <param name="graph">The X6 graph.</param>
    /// <returns>A flowchart activity.</returns>
    Flowchart MapX6Graph(X6Graph graph);
    
}