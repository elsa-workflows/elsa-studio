using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Represents a visitor that can visit an activity.
/// </summary>
public interface IActivityVisitor
{
    /// <summary>
    /// Visits the specified activity and returns all activities in its graph.
    /// </summary>
    /// <param name="activity">The activity to visit.</param>
    /// <returns>A flat list of activities found in the graph.</returns>
    IEnumerable<JsonObject> Visit(JsonObject activity);
}