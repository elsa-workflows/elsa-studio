using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonObject"/> representing an activity
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Gets the flowchart from the specified activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static JsonObject GetFlowchart(this JsonObject activity)
    {
        var typeName = activity.GetTypeName();

        // 1) If it already *is* a flowchart, return it
        if (typeName == "Elsa.Flowchart")
            return activity;

        // 2) If it’s the root workflow, unwrap its root flowchart
        if (typeName == "Elsa.Workflow")
            return activity.GetRoot()!;

        // 3) Look for a child property that *is* a Flowchart
        foreach (var kvp in activity)
        {
            if (kvp.Value is JsonObject child && child.GetTypeName() == "Elsa.Flowchart")
                return child;
        }

        // 4) **NEW** fallback: if there’s an "activities" array, wrap it as a synthetic flowchart
        if (
            activity.TryGetPropertyValue("activities", out var arr)
            && arr is JsonArray innerActivities
        )
        {
            var synth = new JsonObject
            {
                // carry over the container’s id & nodeId so selection still works
                ["id"] = activity["id"]!,
                ["nodeId"] = activity["nodeId"]!,
                ["type"] = "Elsa.Flowchart",
                ["version"] = activity["version"]!,
                ["customProperties"] = new JsonObject(),
                ["metadata"] = new JsonObject(),
                ["activities"] = new JsonArray(innerActivities.ToArray()),
            };

            return synth;
        }

        // 5) nothing matched — truly unsupported
        throw new NotSupportedException(
            $"Activity '{typeName}' does not contain an inner flowchart."
        );
    }
}
