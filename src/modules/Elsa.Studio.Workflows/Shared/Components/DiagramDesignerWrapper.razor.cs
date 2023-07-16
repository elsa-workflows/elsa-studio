using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Args;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

public partial class DiagramDesignerWrapper
{
    private IDiagramDesigner? _diagramDesigner;
    private Stack<ActivityPathSegment> _pathSegments = new();

    [Parameter] public JsonObject Activity { get; set; } = default!;
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public RenderFragment CustomToolbarItems { get; set; } = default!;
    [Parameter] public bool IsProgressing { get; set; }
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }
    [Parameter] public Func<Task>? GraphUpdated { get; set; }
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IActivityIdGenerator ActivityIdGenerator { get; set; } = default!;

    private ActivityPathSegment? CurrentPathSegment => _pathSegments.TryPeek(out var segment) ? segment : default;

    public Task<JsonObject> ReadActivityAsync()
    {
        return Task.FromResult(Activity);
    }

    public Task LoadActivityAsync(JsonObject activity)
    {
        _pathSegments.Clear();
        StateHasChanged();
        return _diagramDesigner!.LoadRootActivity(activity);
    }

    public Task UpdateActivityAsync(string activityId, JsonObject activity)
    {
        return _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    protected override Task OnInitializedAsync()
    {
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(Activity);

        return base.OnInitializedAsync();
    }

    private JsonObject? GetCurrentActivity()
    {
        var currentContainer = Activity;
        var currentActivity = default(JsonObject);

        foreach (var pathSegment in _pathSegments)
        {
            var flowchart = currentContainer;
            var activities = flowchart.GetActivities();
            currentActivity = activities.First(x => x.GetId() == pathSegment.ActivityId);
            var propName = pathSegment.PortName.Camelize();
            currentContainer = currentActivity.GetProperty(propName)!.AsObject();
        }

        return currentActivity;
    }
    
    private JsonObject GetCurrentContainerActivity()
    {
        var currentActivity = Activity;

        foreach (var pathSegment in _pathSegments)
        {
            var flowchart = currentActivity;
            var activities = flowchart.GetActivities();
            var childActivity = activities.First(x => x.GetId() == pathSegment.ActivityId);
            var propName = pathSegment.PortName.Camelize();
            var activity = childActivity.GetProperty(propName)!.AsObject();

            currentActivity = activity;
        }

        return currentActivity;
    }

    private List<BreadcrumbItem> GetBreadcrumbItems()
    {
        return _pathSegments.Select((segment, index) =>
        {
            var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(segment.ActivityType);
            var disabled = index == _pathSegments.Count - 1;
            var item = new BreadcrumbItem(segment.ActivityId, $"#{segment.ActivityId}", disabled, displaySettings.Icon);
            return item;
        }).ToList();
    }

    private async Task OnActivityEmbeddedPortSelected(ActivityEmbeddedPortSelectedArgs args)
    {
        var activity = args.Activity;
        var embeddedActivity = activity.GetProperty(args.PortName)?.AsObject();

        if (embeddedActivity != null)
        {
            // If the embedded activity has no designer support, then open it in the activity properties editor by raising the ActivitySelected event.
            if (embeddedActivity.GetTypeName() != "Elsa.Flowchart")
            {
                ActivitySelected?.Invoke(embeddedActivity);
                return;
            }
        }
        else
        {
            // Create a flowchart and embed it into the activity.
            embeddedActivity = new JsonObject(new Dictionary<string, JsonNode?>
            {
                ["id"] = ActivityIdGenerator.GenerateId(),
                ["type"] = "Elsa.Flowchart",
                ["version"] = 1,
                ["name"] = "Flowchart1",
            });
            var propName = args.PortName.Camelize();
            activity[propName] = embeddedActivity;
            
            // Update the graph in the designer.
            await _diagramDesigner!.UpdateActivityAsync(activity.GetId(), activity);
        }
        
        // Create a new path segment of the container activity and push it onto the stack.
        var segment = new ActivityPathSegment(activity.GetId(), activity.GetTypeName(), args.PortName);
        _pathSegments.Push(segment);
        
        // Display the current segment in the designer.
        var currentActivity = GetCurrentContainerActivity();
        await _diagramDesigner!.LoadRootActivity(currentActivity);

        StateHasChanged();
    }

    private async Task OnGraphUpdated()
    {
        var rootActivity = await _diagramDesigner!.ReadRootActivityAsync();
        var currentActivity = GetCurrentActivity();
        var currentSegment = CurrentPathSegment;

        if (currentActivity == null || currentSegment == null)
            Activity = rootActivity;
        else
        {
            var propName = currentSegment.PortName.Camelize();
            currentActivity[propName] = rootActivity;
        }
        
        if (GraphUpdated != null)
            await GraphUpdated();
    }

    private Task OnBreadcrumbItemClicked(BreadcrumbItem item)
    {
        var activityId = item.Href[1..];

        while (_pathSegments.TryPop(out var segment))
        {
            if (segment.ActivityId == activityId)
                break;
        }

        StateHasChanged();
        return Task.CompletedTask;
    }
}