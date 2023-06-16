using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Extensions;
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

        foreach (var activityElement in activityElements)
        {
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
        var metadataElement = activityElement.TryGetPropertySafe("metadata", out var metadataProp) ? metadataProp : default;
        var designerElement = metadataElement.TryGetPropertySafe("designer", out var designerProp) ? designerProp : default;
        var positionElement = designerElement.TryGetPropertySafe("position", out var positionProp) ? positionProp : default;
        var sizeElement = designerElement.TryGetPropertySafe("size", out var sizeProp) ? sizeProp : default;
        var x = positionElement.TryGetDoubleSafe("x", out var xProp) ? xProp : 0;
        var y = positionElement.TryGetDoubleSafe("y", out var yProp) ? yProp : 0;
        var width = sizeElement.TryGetDoubleSafe("width", out var widthProp) ? widthProp : 0;
        var height = sizeElement.TryGetDoubleSafe("height", out var heightProp) ? heightProp : 0;

        if (width == 0) width = 200;
        if (height == 0) height = 50;

        var node = new X6Node
        {
            Id = activityId,
            Data = activityElement,
            Size = new X6Size(width, height),
            Position = new X6Position(x, y),
            Shape = "elsa-activity"
        };

        // Create default input port.
        node.Ports.Items.Add(new X6Port
        {
            Id = "In",
            Group = "in",
        });

        // Create output ports.
        foreach (var port in activityDescriptor.Ports)
        {
            node.Ports.Items.Add(new X6Port
            {
                Id = port.Name,
                Group = "out",
            });
        }

        // If there is no output port, create a default one.
        if (node.Ports.Items.All(port => port.Group != "out"))
        {
            node.Ports.Items.Add(new X6Port
            {
                Id = "Done",
                Group = "out",
            });
        }

        return node;
    }

    public JsonElement MapX6Graph(X6Graph graph)
    {
        var activityElements = new JsonArray();
        var connectionElements = new JsonArray();

        foreach (var node in graph.Nodes)
        {
            var activityElement = JsonObject.Create(node.Data)!;
            var metadataElement = activityElement["metadata"] ?? new JsonObject();
            var designerElement = metadataElement["designer"] ?? new JsonObject();

            designerElement["position"] = new JsonObject
            {
                ["x"] = node.Position.X,
                ["y"] = node.Position.Y
            };

            designerElement["size"] = new JsonObject
            {
                ["width"] = node.Size.Width,
                ["height"] = node.Size.Height
            };

            metadataElement["designer"] = designerElement;
            activityElement["metadata"] = metadataElement;

            activityElements.Add(activityElement);
        }

        foreach (var edge in graph.Edges)
        {
            var connectionElement = new JsonObject
            {
                ["source"] = edge.Source.Cell,
                ["sourcePort"] = edge.Source.Port,
                ["target"] = edge.Target.Cell,
                ["targetPort"] = edge.Target.Port
            };
            connectionElements.Add(connectionElement);
        }

        var flowchartElement = new JsonObject
        {
            ["activities"] = activityElements,
            ["connections"] = connectionElements
        };

        return JsonSerializer.SerializeToElement(flowchartElement);
    }
}