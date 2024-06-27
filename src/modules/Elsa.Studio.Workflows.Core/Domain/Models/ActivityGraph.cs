using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// An activity and a lookup of its descendants.
public class ActivityGraph(JsonObject activity, IDictionary<string, ActivityNode> activityNodeLookup)
{
    public JsonObject Activity { get; } = activity;
    public IDictionary<string, ActivityNode> ActivityNodeLookup { get; private set; } = activityNodeLookup;
    
    /// Merges the specified subgraph into the current graph.
    public void Merge(ActivityNode nodeSubgraph)
    {
        var currentNodes = ActivityNodeLookup;
        var addedNodes = nodeSubgraph.Flatten().ToDictionary(x => x.NodeId, x => x);
        var mergedNodes = new Dictionary<string, ActivityNode>(currentNodes);

        foreach (var kvp in addedNodes) mergedNodes[kvp.Key] = kvp.Value;

        ActivityNodeLookup = mergedNodes;
    }
}