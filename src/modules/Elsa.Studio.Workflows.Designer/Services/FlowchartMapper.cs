using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class FlowchartMapper : IFlowchartMapper
{
    private readonly IActivityMapper _activityMapper;

    public FlowchartMapper(IActivityMapper activityMapper)
    {
        _activityMapper = activityMapper;
    }

    public X6Graph Map(Flowchart flowchart)
    {
        var graph = new X6Graph();

        foreach (var activity in flowchart.Activities)
        {
            var node = _activityMapper.MapActivity(activity);
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

    public Flowchart Map(X6Graph graph)
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