using System.Text.Json;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class DefaultFlowchartMapper : IFlowchartMapper
{
    private readonly DesignerJsInterop _designerJsInterop;
    private readonly IActivityRegistry _activityRegistry;

    public DefaultFlowchartMapper(DesignerJsInterop designerJsInterop, IActivityRegistry activityRegistry)
    {
        _designerJsInterop = designerJsInterop;
        _activityRegistry = activityRegistry;
    }
    
    public async Task<X6Graph> MapAsync(JsonElement flowchartElement, CancellationToken cancellationToken = default)
    {
        var graph = new X6Graph();
        var activityElements = flowchartElement.GetProperty("activities").EnumerateArray();
        var connectionElements = flowchartElement.GetProperty("connections").EnumerateArray();
        var activityDescriptors = (await _activityRegistry.ListAsync(cancellationToken)).ToDictionary(x => x.TypeName);

        foreach (var activityElement in activityElements)
        {
            var activityId = activityElement.GetProperty("id").GetString()!;
            var activityType = activityElement.GetProperty("type").GetString()!;
            var activityDescriptor = activityDescriptors[activityType];
            var metadataElement = activityElement.GetProperty("metadata");
            var designerElement = metadataElement.GetProperty("designer");
            var positionElement = designerElement.GetProperty("position");
            var x = positionElement.GetProperty("x").GetInt32();
            var y = positionElement.GetProperty("y").GetInt32();
            //var activitySize = await _designerJsInterop.CalculateActivitySizeAsync(activityElement);

            var node = new X6Node
            {
                Id = activityId,
                Data = activityId,
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
}