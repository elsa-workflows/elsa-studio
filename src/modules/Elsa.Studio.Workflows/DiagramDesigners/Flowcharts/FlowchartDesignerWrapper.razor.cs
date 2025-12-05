using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Args;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A wrapper around the <see cref="FlowchartDesigner"/> component that provides interactivity such as drag and drop and displaying the designer in edit or read-only mode.
/// </summary>
public partial class FlowchartDesignerWrapper
{
    /// <summary>
    /// The flowchart to display.
    /// </summary>
    [Parameter] public JsonObject Flowchart { get; set; } = null!;

    /// <summary>
    /// A map of activity stats.
    /// </summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// <summary>
    /// Whether the designer is read-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <summary>
    /// An event raised when an activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    [Parameter] public EventCallback<JsonObject> ActivityUpdated { get; set; }

    /// <summary>
    /// An event raised when an embedded port is selected.
    /// </summary>
    [Parameter] public EventCallback<ActivityEmbeddedPortSelectedArgs> ActivityEmbeddedPortSelected { get; set; }

    /// <summary>
    /// An event raised when an activity is double-clicked.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }

    /// <summary>
    /// An event raised when the graph is updated.
    /// </summary>
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [CascadingParameter] private DragDropManager DragDropManager { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    private FlowchartDesigner Designer { get; set; } = null!;

    private string? _lastFlowchartId;

    /// <summary>
    /// Loads the specified flowchart activity into the designer.
    /// </summary>
    /// <param name="activity">The flowchart activity to load.</param>
    /// <param name="activityStats">A map of activity stats.</param>
    public async Task LoadFlowchartAsync(
        JsonObject activity,
        IDictionary<string, ActivityStats>? activityStats = null
    )
    {
        // 1) Unwrap any container to the real Elsa.Flowchart
        var flowchart = activity.GetFlowchart() ?? activity.FindActivitiesContainer();

        if (flowchart == null)
            return;
        // 2) Bail out if it's the exact same chart we already have
        var id = flowchart.GetId();
        if (id == _lastFlowchartId)
            return;

        // 3) Otherwise record and hand off
        _lastFlowchartId = id;
        Flowchart = flowchart;
        ActivityStats = activityStats;

        await Designer.LoadFlowchartAsync(flowchart, activityStats);
    }

    /// <summary>
    /// Updates the specified activity in the flowchart.
    /// </summary>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    /// <exception cref="InvalidOperationException">Thrown if the designer is read-only.</exception>
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update activity because the designer is read-only.");

        if (activity != Flowchart)
            await Designer.UpdateActivityAsync(id, activity);
    }

    /// <summary>
    /// Updates the stats of the specified activity.
    /// </summary>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="stats">The stats to update.</param>
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats) => await Designer.UpdateActivityStatsAsync(id, stats);

    /// <summary>
    /// Selects the specified activity in the flowchart.
    /// </summary>
    /// <param name="id">The ID of the activity to select.</param>
    public async Task SelectActivityAsync(string id) => await Designer.SelectActivityAsync(id);

    /// <summary>
    /// Reads the root activity from the flowchart.
    /// </summary>
    /// <returns>The root activity.</returns>
    public async Task<JsonObject> ReadRootActivityAsync() => await Designer.ReadFlowchartAsync();

    /// <summary>
    /// Zooms the designer to fit the content.
    /// </summary>
    public async Task ZoomToFitAsync() => await Designer.ZoomToFitAsync();

    /// <summary>
    /// Centers the content of the designer.
    /// </summary>
    public async Task CenterContentAsync() => await Designer.CenterContentAsync();

    /// <summary>
    /// Exports the graphs content to a supplied format.
    /// </summary>
    /// <param name="captureOptions">The capture options</param>
    /// <returns></returns>
    public async Task ExportContentToFormatAsync(CaptureOptions captureOptions) => await Designer.ExportContentToFormatAsync(captureOptions);

    /// <summary>
    /// Auto layouts the flowchart.
    /// </summary>
    public async Task AutoLayoutAsync() => await Designer.AutoLayoutAsync(Flowchart, ActivityStats);

    private async Task AddNewActivityAsync(ActivityDescriptor activityDescriptor, double x, double y)
    {
        var activities = Flowchart.GetActivities().ToList();
        var newActivityId = IdentityGenerator.GenerateId();

        var newActivity = new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["id"] = newActivityId,
            ["nodeId"] = $"{Flowchart.GetNodeId()}:{newActivityId}",
            ["name"] = ActivityNameGenerator.GenerateNextName(activities, activityDescriptor),
            ["type"] = activityDescriptor.TypeName,
            ["version"] = activityDescriptor.Version,
        });

        newActivity.SetDesignerMetadata(new()
        {
            Position = new(x, y)
        });

        // Copy constructor values from the activity descriptor.
        foreach (var property in activityDescriptor.ConstructionProperties)
        {
            var valueNode = JsonSerializer.SerializeToNode(property.Value);
            var propertyName = property.Key.Camelize();
            newActivity.SetProperty(valueNode, propertyName);
        }

        // If the activity is a trigger, and it's the first trigger on the flowchart, set the trigger property to true.
        if (activityDescriptor.Kind == ActivityKind.Trigger && activities.All(activity => activity.GetCanStartWorkflow() != true))
            newActivity.SetCanStartWorkflow(true);

        await Designer.AddActivityAsync(newActivity);

        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(newActivity);
    }

    private void OnDragOver(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor)
        {
            e.DataTransfer.DropEffect = "none";
            return;
        }

        e.DataTransfer.DropEffect = "move";
    }

    private async Task OnDrop(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor activityDescriptor)
            return;

        var x = e.PageX;
        var y = e.PageY;

        await AddNewActivityAsync(activityDescriptor, x, y);
    }

    private async Task OnCanvasSelected()
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(Flowchart);
    }
}