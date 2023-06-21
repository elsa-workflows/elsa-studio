using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
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

    public X6Graph MapFlowchart(Flowchart flowchart)
    {
        var graph = new X6Graph();

        foreach (var activity in flowchart.Activities)
        {
            var node = MapActivity(activity);
            graph.Nodes.Add(node);
        }

        foreach (var connection in flowchart.Connections)
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

    public X6Node MapActivity(Activity activity)
    {
        var activityType = activity.Type;
        var activityDescriptor = _activityDescriptors[activityType];
        var activityId = activity.Id;
        var designerMetadata = activity.GetDesignerMetadata();
        var position = designerMetadata?.Position;
        var size = designerMetadata?.Size;
        var x = position?.X ?? 0;
        var y = position?.Y ?? 0;
        var width = size?.Width ?? 0;
        var height = size?.Height ?? 0;

        if (width == 0) width = 200;
        if (height == 0) height = 50;

        var node = new X6Node
        {
            Id = activityId,
            Data = activity,
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

    public Flowchart MapX6Graph(X6Graph graph)
    {
        var activities = new List<Activity>();
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

        var flowchart = new Flowchart
        {
            Activities = activities,
            Connections = connections
        };

        return flowchart;
    }
}