using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Resolvers;

/// <summary>
/// A default resolver.
/// </summary>
public class DefaultActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => -1;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => true;

    /// <inheritdoc />
    public ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(
        JsonObject activity,
        CancellationToken cancellationToken = default
    )
    {
        var embedded = new List<EmbeddedActivity>();

        foreach (var (propName, node) in activity)
        {
            switch (node)
            {
                // 1) direct child activity:
                case JsonObject childObj when childObj.IsActivity():
                    embedded.Add(new EmbeddedActivity(childObj, propName));
                    break;

                // 2) array of activities:
                case JsonArray arr:
                    foreach (var item in arr.OfType<JsonObject>())
                    {
                        // e.g. expectedStatusCodes: pick up item.activity
                        if (
                            item.TryGetPropertyValue("activity", out var actNode)
                            && actNode is JsonObject actObj
                            && actObj.IsActivity()
                        )
                        {
                            embedded.Add(new EmbeddedActivity(actObj, propName));
                        }
                        else if (item.IsActivity())
                        {
                            embedded.Add(new EmbeddedActivity(item, propName));
                        }
                    }
                    break;
            }

            // 3) special‑case any nested Flowchart under "body":
            if (
                node is JsonObject container
                && container.TryGetPropertyValue("body", out var bodyNode)
                && bodyNode is JsonObject bodyObj
                && bodyObj.TryGetPropertyValue("activities", out var activitiesNode)
                && activitiesNode is JsonArray childActivities
            )
            {
                foreach (var child in childActivities.OfType<JsonObject>())
                    embedded.Add(new EmbeddedActivity(child, propName));
            }
        }

        return new ValueTask<IEnumerable<EmbeddedActivity>>(embedded);
    }
}
