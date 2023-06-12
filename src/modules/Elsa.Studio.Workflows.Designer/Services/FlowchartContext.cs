using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

public class FlowchartContext
{
    public static FlowchartContext Current { get; set; } = default!;

    public static void Setup(JsonElement flowchart, IDictionary<string, ActivityDescriptor> activityDescriptors)
    {
        Current = Create(flowchart, activityDescriptors);
    }

    public static FlowchartContext Create(JsonElement flowchart, IDictionary<string, ActivityDescriptor> activityDescriptors)
    {
        return new FlowchartContext
        {
            Flowchart = flowchart,
            ActivityDescriptors = activityDescriptors,
            Activities = flowchart.GetProperty("activities").EnumerateArray().ToDictionary(x => x.GetProperty("id").GetString()!, x => x),
            Connections = flowchart.GetProperty("connections").EnumerateArray().ToList()
        };
    }

    public JsonElement Flowchart { get; set; } = default!;
    public IDictionary<string, ActivityDescriptor> ActivityDescriptors { get; set; } = default!;
    public IDictionary<string, JsonElement> Activities { get; private set; } = default!;
    public ICollection<JsonElement> Connections { get; private set; } = default!;
}