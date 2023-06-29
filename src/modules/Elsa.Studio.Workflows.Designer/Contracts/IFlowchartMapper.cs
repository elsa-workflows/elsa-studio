using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a Flowchart activity from and to an X6Graph.
/// </summary>
public interface IFlowchartMapper
{
    /// <summary>
    /// Maps a flowchart activity to an X6Graph.
    /// </summary>
    /// <param name="flowchart">The flowchart activity.</param>
    X6Graph Map(Flowchart flowchart);
    
    /// <summary>
    /// Maps an X6 graph to a flowchart activity.
    /// </summary>
    /// <param name="graph">The X6 graph.</param>
    Flowchart Map(X6Graph graph);
    
}