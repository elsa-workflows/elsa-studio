using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class DefaultFlowchartMapper : IFlowchartMapper
{
    private readonly IDictionary<string, ActivityDescriptor> _activityDescriptors;

    public DefaultFlowchartMapper(IDictionary<string, ActivityDescriptor> activityDescriptors)
    {
        _activityDescriptors = activityDescriptors;
    }

    public X6Graph MapFlowchart(JsonElement flowchartElement)
    {
        var graph = new X6Graph();
        var activityElements = flowchartElement.GetProperty("activities").EnumerateArray();
        var connectionElements = flowchartElement.GetProperty("connections").EnumerateArray();
        var activityDescriptors = _activityDescriptors;

        foreach (var activityElement in activityElements)
        {
            var activityType = activityElement.GetProperty("type").GetString()!;
            var node = MapActivity(activityElement);
            graph.Nodes.Add(node);
        }

        foreach (var connectionElement in connectionElements)
        {
            var sourceId = connectionElement.GetProperty("source").GetString()!;
            var sourcePort = connectionElement.GetProperty("sourcePort").GetString()!;
            var targetId = connectionElement.GetProperty("target").GetString()!;
            var targetPort = connectionElement.GetProperty("targetPort").GetString()!;
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

    public X6Node MapActivity(JsonElement activityElement)
    {
        var activityType = activityElement.GetProperty("type").GetString()!;
        var activityDescriptor = _activityDescriptors[activityType];
        var activityId = activityElement.GetProperty("id").GetString()!;
        var metadataElement = activityElement.GetProperty("metadata");
        var designerElement = metadataElement.GetProperty("designer");
        var positionElement = designerElement.GetProperty("position");
        var x = positionElement.GetProperty("x").GetInt32();
        var y = positionElement.GetProperty("y").GetInt32();

        var node = new X6Node
        {
            Id = activityId,
            Data = activityElement,
            Width = 200,
            Height = 50,
            Shape = "elsa-activity",
            X = x,
            Y = y
        };

        // Create default input port.
        node.Ports.Add(new X6Port
        {
            Id = "In",
            Group = "in",
        });

        // Create output ports.
        foreach (var port in activityDescriptor.Ports)
        {
            node.Ports.Add(new X6Port
            {
                Id = port.Name,
                Group = "out",
            });
        }

        // If there is no output port, create a default one.
        if (node.Ports.All(port => port.Group != "out"))
        {
            node.Ports.Add(new X6Port
            {
                Id = "Done",
                Group = "out",
            });
        }

        return node;
    }
}