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

        foreach (var kvp in addedNodes)
        {
            // If the key already exists, use the value that contains the most elements, because that means it has a subgraph.
            // This happens when an activity is linked to a workflow definition.
            var nodeToUse = kvp.Value;
            if (mergedNodes.TryGetValue(kvp.Key, out var existingNode))
            {
                var addedNode = kvp.Value;
                nodeToUse = addedNode.Activity.Count > existingNode.Activity.Count ? addedNode : existingNode;
            }

            mergedNodes[kvp.Key] = nodeToUse;
        }

        ActivityNodeLookup = mergedNodes;
    }
}