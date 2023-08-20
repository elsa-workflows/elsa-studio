using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;

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
        var activityTypeName = activity.GetTypeName();

        if (activityTypeName == "Elsa.Flowchart")
            return activity;

        if (activityTypeName == "Elsa.Workflow")
            return activity.GetRoot()!;

        throw new NotSupportedException();
    }
}