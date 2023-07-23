using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Extensions;

public static class ActivityVisitorExtensions
{
    /// <summary>
    /// A method that uses the visitor and returns a lookup dictionary by activity ID.
    /// </summary>
    public static IDictionary<string, JsonObject> VisitAndMap(this IActivityVisitor visitor, JsonObject activity)
    {
        var activities = visitor.Visit(activity);
        return activities.ToDictionary(x => x.GetId());
    }
}