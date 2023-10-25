using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// A set of extensions for <see cref="IActivityVisitor"/>.
/// </summary>
public static class ActivityVisitorExtensions
{
    /// <summary>
    /// A method that uses the visitor and returns a lookup dictionary by activity ID.
    /// </summary>
    public static async Task<IDictionary<string, ActivityNode>> VisitAndMapAsync(this IActivityVisitor visitor, WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = ToActivity(workflowDefinition);
        return await visitor.VisitAndMapAsync(workflowActivity);
    }
    
    /// <summary>
    /// A method that uses the visitor and returns a lookup dictionary by activity ID.
    /// </summary>
    public static async Task<ActivityNode> VisitAsync(this IActivityVisitor visitor, WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = ToActivity(workflowDefinition);
        return await visitor.VisitAsync(workflowActivity);
    }
    
    /// <summary>
    /// A method that uses the visitor and returns a lookup dictionary by activity ID.
    /// </summary>
    public static async Task<IDictionary<string, ActivityNode>> VisitAndMapAsync(this IActivityVisitor visitor, JsonObject activity)
    {
        var graph = await visitor.VisitAsync(activity);
        var nodes = graph.Flatten();
        return nodes.ToDictionary(x => x.NodeId);
    }
    
    /// <summary>
    /// Creates an activity from the specified workflow definition.
    /// </summary>
    private static JsonObject ToActivity(WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = new JsonObject();
        workflowActivity.SetId("Workflow1");
        workflowActivity.SetTypeName("Elsa.Workflow");
        workflowActivity.SetVersion(1);
        workflowActivity.SetName(workflowDefinition.Name);
        workflowActivity.SetRoot(workflowDefinition.Root);
        return workflowActivity;
    }

}