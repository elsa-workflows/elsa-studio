using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// An activity and a lookup of its descendants.
public record ActivityGraph(JsonObject Activity, IDictionary<string, ActivityNode> ActivityNodeLookup)
{
    /// Merges the specified subgraph into the current graph.
    public ActivityGraph Merge(ActivityNode nodeSubgraph)
    {
        var currentNodes = ActivityNodeLookup;
        var addedNodes = nodeSubgraph.Flatten().ToDictionary(x => x.NodeId, x => x);
        var mergedNodes = new Dictionary<string, ActivityNode>(currentNodes);

        foreach (var kvp in addedNodes) mergedNodes[kvp.Key] = kvp.Value;
        
        return this with { ActivityNodeLookup = mergedNodes };
    }
}