using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
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
    public static async Task<IDictionary<string, ActivityNode>> VisitAndMapAsync(this IActivityVisitor visitor, JsonObject activity)
    {
        var graph = await visitor.VisitAsync(activity);
        var nodes = graph.Flatten();
        return nodes.ToDictionary(x => x.Activity.GetId());
    }
}