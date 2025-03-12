using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class FlowchartMapper(IActivityMapper activityMapper) : IFlowchartMapper
{
    public X6Graph Map(JsonObject flowchart, IDictionary<string, ActivityStats>? activityStatsMap = default)
    {
        var graph = new X6Graph();
        var activities = flowchart.GetActivities();
        var connections = flowchart.GetConnections();

        foreach (var activity in activities)
        {
            var activityNodeId = activity.GetNodeId();
            var activityStats = activityStatsMap?.TryGetValue(activityNodeId, out var stats) == true ? stats : null;
            var node = activityMapper.MapActivity(activity, activityStats);
            graph.Nodes.Add(node);
        }

        foreach (var connection in connections)
        {
            var source = connection.Source;
            var target = connection.Target;
            var sourceId = source.ActivityId;
            var sourcePort = source.Port ?? "Done";
            var targetId = target.ActivityId;
            var targetPort = target.Port ?? "In";
            var connector = new X6Edge
            {
                Shape = "elsa-edge",
                Source = new X6Endpoint
                {
                    Cell = sourceId,
                    Port = sourcePort
                },
                Target = new X6Endpoint
                {
                    Cell = targetId,
                    Port = targetPort
                }
            };

            graph.Edges.Add(connector);
        }

        return graph;
    }

    public JsonObject Map(X6Graph graph)
    {
        var activities = new List<JsonObject>();
        var connections = new List<Connection>();

        foreach (var node in graph.Nodes)
        {
            var activity = node.Data;
            var designerMetadata = activity.GetDesignerMetadata();

            designerMetadata.Position = new Position
            {
                X = node.Position.X,
                Y = node.Position.Y
            };

            designerMetadata.Size = new Size
            {
                Width = node.Size.Width,
                Height = node.Size.Height
            };

            activity.SetDesignerMetadata(designerMetadata);
            activities.Add(activity);
        }

        foreach (var edge in graph.Edges)
        {
            var connection = new Connection
            {
                Source = new Endpoint(edge.Source.Cell, edge.Source.Port),
                Target = new Endpoint(edge.Target.Cell, edge.Target.Port)
            };
            connections.Add(connection);
        }

        var flowchart = new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["activities"] = activities.SerializeToArray(),
            ["connections"] = connections.SerializeToArray()
        });

        return flowchart;
    }
}