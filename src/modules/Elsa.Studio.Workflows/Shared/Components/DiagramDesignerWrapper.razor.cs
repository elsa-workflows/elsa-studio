using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

/// <summary>
/// A wrapper around the diagram designer that provides a breadcrumb and a toolbar.
/// </summary>
public partial class DiagramDesignerWrapper
{
    private IDiagramDesigner? _diagramDesigner;
    private Stack<ActivityPathSegment> _pathSegments = new();
    private List<BreadcrumbItem> _breadcrumbItems = new();
    private IDictionary<string, ActivityStats> _activityStats = new Dictionary<string, ActivityStats>();

    /// <summary>
    /// The workflow definition version ID.
    /// </summary>
    [Parameter] public string WorkflowDefinitionVersionId { get; set; } = default!;

    /// <summary>
    /// The diagram activity to display.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = default!;

    /// <summary>
    /// Whether the designer is read-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <summary>
    /// The workflow instance ID, if any.
    /// </summary>
    [Parameter] public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// A custom toolbar to display.
    /// </summary>
    [Parameter] public RenderFragment? CustomToolbarItems { get; set; }

    /// <summary>
    /// Whether the designer is progressing.
    /// </summary>
    [Parameter] public bool IsProgressing { get; set; }

    /// <summary>
    /// An event raised when an activity is selected.
    /// </summary>
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    /// <summary>
    /// An event raised when an embedded port is selected.
    /// </summary>
    [Parameter] public Func<Task>? GraphUpdated { get; set; }

    /// <summary>
    /// An event raised when the path changes.
    /// </summary>
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }

    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IActivityPortService ActivityPortService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = default!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private ActivityPathSegment? CurrentPathSegment => _pathSegments.TryPeek(out var segment) ? segment : default;

    /// <summary>
    /// Selects the activity with the specified ID.
    /// </summary>
    /// <param name="activityNode">The activity node to select.</param>
    public async Task SelectActivityAsync(ActivityNode activityNode)
    {
        var diagramActivity = GetCurrentContainerActivity();
        var activityToSelect = diagramActivity.GetActivities()
            .FirstOrDefault(x => x.GetId() == activityNode.Activity.GetId());

        if (activityToSelect != null)
        {
            await _diagramDesigner!.SelectActivityAsync(activityNode.Activity.GetId());
            return;
        }

        // The selected activity is not a direct child of the current container.
        // We need to find the container that owns the activity and update the path segments accordingly.
        var embeddedActivityNode = activityNode;

        var path = new List<ActivityPathSegment>();

        while (true)
        {
            // TODO: The following process is highly specialized for the case of Flowchart diagrams and will not work for other diagram types.
            // The key is to find the owning activity of the activity that was selected. Which in the case of the Flowchart diagram, has at least a Flowchart as its parent, which in turn my have a Workflow as its parent.

            // Find the flowchart to which this activity belongs.
            var flowchart = embeddedActivityNode.Ancestors()
                .FirstOrDefault(x => x.Activity.GetTypeName() == "Elsa.Flowchart");

            // Try to get the owning activity of the flowchart. Keep in mind that there could be a Workflow activity in between if the owning activity is a WorkflowDefinitionActivity.
            var owningActivityNode =
                flowchart?.Ancestors().FirstOrDefault(x => x.Activity.GetTypeName() != "Elsa.Workflow");

            if (owningActivityNode == null || !owningActivityNode.Parents.Any())
                break;

            var portName = flowchart!.Port!.Pascalize();
            path.Add(new ActivityPathSegment(owningActivityNode.Activity.GetId(),
                owningActivityNode.Activity.GetTypeName(), portName));

            embeddedActivityNode = owningActivityNode;
        }

        // Update the path segments.
        await UpdatePathSegmentsAsync(segments =>
        {
            segments.Clear();

            foreach (var segment in path.AsEnumerable().Reverse())
                segments.Push(segment);
        });

        // Display new segment.
        await DisplayCurrentSegmentAsync();

        // Select the activity.
        await _diagramDesigner!.SelectActivityAsync(activityNode.Activity.GetId());
    }

    /// <summary>
    /// Updates the stats of the specified activity.
    /// </summary>
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="stats">The stats to update.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        await _diagramDesigner!.UpdateActivityStatsAsync(activityId, stats);
    }

    /// <summary>
    /// Reads the activity from the designer.
    /// </summary>
    public Task<JsonObject> ReadActivityAsync()
    {
        return Task.FromResult(Activity);
    }

    /// <summary>
    /// Loads the specified activity into the designer.
    /// </summary>
    /// <param name="activity">The activity to load.</param>
    public async Task LoadActivityAsync(JsonObject activity)
    {
        if (_diagramDesigner != null)
        {
            await _diagramDesigner!.LoadRootActivityAsync(activity, _activityStats);
            await UpdatePathSegmentsAsync(segments => segments.Clear());
        }
    }

    /// <summary>
    /// Updates the specified activity in the designer.
    /// </summary>
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    public async Task UpdateActivityAsync(string activityId, JsonObject activity)
    {
        var currentContainer = GetCurrentContainerActivity();

        if (currentContainer == activity)
        {
            if (GraphUpdated != null)
                await GraphUpdated();

            return;
        }

        await _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(Activity);
        await UpdatePathSegmentsAsync(segments => segments.Clear());
    }

    private JsonObject? GetCurrentActivity()
    {
        var resolvedPath = ResolvePath().LastOrDefault();
        return resolvedPath?.Activity;
    }

    private JsonObject GetCurrentContainerActivity()
    {
        var path = ResolvePath().ToList();
        var segment = path.LastOrDefault();
        return segment?.EmbeddedActivity ?? Activity ?? throw new Exception("No container activity found");
    }

    private IEnumerable<GraphSegment> ResolvePath()
    {
        var currentContainer = Activity;

        foreach (var pathSegment in _pathSegments.Reverse())
        {
            var flowchart = currentContainer.GetFlowchart();
            var activities = flowchart.GetActivities();
            var currentActivity = activities.FirstOrDefault(x => x.GetId() == pathSegment.ActivityId);

            if (currentActivity == null)
                continue;

            var portName = pathSegment.PortName;
            var activityTypeName = currentActivity.GetTypeName();
            var activityVersion = currentActivity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, currentActivity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);

            currentContainer = portProvider.ResolvePort(portName, portProviderContext)!.GetFlowchart();

            var segment = new GraphSegment(currentActivity, pathSegment.PortName, currentContainer);
            yield return segment;
        }
    }

    private async Task UpdatePathSegmentsAsync(Action<Stack<ActivityPathSegment>> action)
    {
        action(_pathSegments);
        await UpdateBreadcrumbItemsAsync();
    }

    private async Task UpdateBreadcrumbItemsAsync()
    {
        var currentContainerActivity = GetCurrentContainerActivity();
        var currentActivity = GetCurrentActivity();
        _breadcrumbItems = (await GetBreadcrumbItems()).ToList();

        if (WorkflowInstanceId != null)
        {
            var report = await InvokeWithBlazorServiceContext(() => ActivityExecutionService.GetReportAsync(WorkflowInstanceId, currentContainerActivity));
            _activityStats = report.Stats.ToDictionary(x => x.ActivityNodeId, x => new ActivityStats
            {
                Faulted = x.IsFaulted,
                Blocked = x.IsBlocked,
                Completed = x.CompletedCount,
                Started = x.StartedCount,
                Uncompleted = x.UncompletedCount
            });
        }

        if (PathChanged.HasDelegate)
            await PathChanged.InvokeAsync(new DesignerPathChangedArgs(currentContainerActivity, currentActivity));

        StateHasChanged();
    }

    private Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbItems()
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        if (_pathSegments.Any())
            breadcrumbItems.Add(new BreadcrumbItem("Root", "#_root_", false, Icons.Material.Outlined.Home));

        var resolvedPath = ResolvePath().ToList();

        foreach (var segment in resolvedPath)
        {
            var currentActivity = segment.Activity;
            var activityTypeName = currentActivity.GetTypeName();
            var activityVersion = currentActivity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, currentActivity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);
            var ports = portProvider.GetPorts(portProviderContext);
            var embeddedPort = ports.First(x => x.Name == segment.PortName);
            var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityTypeName);
            var disabled = segment == resolvedPath.Last();
            var activityDisplayText = currentActivity.GetName() ?? activityDescriptor.DisplayName;
            var breadcrumbDisplayText = $"{activityDisplayText}: {embeddedPort.DisplayName}";
            var activityBreadcrumbItem = new BreadcrumbItem(breadcrumbDisplayText, $"#{currentActivity.GetId()}",
                disabled, displaySettings.Icon);

            breadcrumbItems.Add(activityBreadcrumbItem);
        }

        return Task.FromResult<IEnumerable<BreadcrumbItem>>(breadcrumbItems);
    }

    private async Task DisplayCurrentSegmentAsync()
    {
        var currentContainerActivity = GetCurrentContainerActivity();

        if (_diagramDesigner == null)
            return;

        await _diagramDesigner.LoadRootActivityAsync(currentContainerActivity, _activityStats);
    }

    private async Task OnActivityDoubleClick(JsonObject activity)
    {
        if (!IsReadOnly)
            return;

        // If the activity is a workflow definition activity, then open the workflow definition editor.
        if (activity.GetWorkflowDefinitionId() != null)
        {
            await OnActivityEmbeddedPortSelected(new ActivityEmbeddedPortSelectedArgs(activity, "Root"));
        }
    }

    // Stitching is the act of converting a graph model of nodes to a structured model where child activities are properties of their parent activity.
    // An example:

    // Source model (the graph):                          Target model:
    // {                                                   {
    //   "activity": {                                        "type": "Elsa.If",
    //     "type": "Elsa.If"                                  "then": {
    //   },                                                     "type": "Elsa.WriteLine",
    //   "children": [                                           "text": "Foo"
    //     {                                                   },
    //       "port": "then",                                   "else": {
    //       "activity": {                                       "type": "Elsa.WriteLine",
    //         "type": "Elsa.WriteLine",                         "text": "Bar"
    //         "text": "Foo"                                   }
    //       }                                             }
    //     }, 
    //     { 
    //       "port": "else", 
    //       "activity": { 
    //         "type": "Elsa.WriteLine", 
    //         "text": "Bar" 
    //       } 
    //     } 
    //   ] 
    // }
    private void StitchNodesRecursive(ActivityNode activityNode)
    {
        var currentNode = activityNode;
        var groupedChildren = currentNode.Children.GroupBy(x => x.Port).ToList();

        foreach (var grouping in groupedChildren)
        {
            var portName = grouping.Key.Camelize();
            var isCollection = grouping.Count() > 1 || portName == "activities";
            currentNode.Activity[portName] = isCollection
                ? new JsonArray(grouping.Select(x => (JsonNode)x.Activity).ToArray())
                : grouping.First().Activity;

            foreach (var childNode in grouping.ToList())
                StitchNodesRecursive(childNode);
        }
    }

    private async Task OnActivityEmbeddedPortSelected(ActivityEmbeddedPortSelectedArgs args)
    {
        var nodes = (await ActivityVisitor.VisitAsync(Activity)).Flatten().ToList();
        var selectedActivity = args.Activity;
        var activity = nodes.First(x => x.NodeId == selectedActivity.GetNodeId()).Activity;
        var portName = args.PortName;
        var activityTypeName = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
        var portProviderContext = new PortProviderContext(activityDescriptor, activity);
        var portProvider = ActivityPortService.GetProvider(portProviderContext);
        var embeddedActivity = portProvider.ResolvePort(portName, portProviderContext);

        if (embeddedActivity == null)
        {
            // Lazy load.
            if (activityDescriptor.CustomProperties.ContainsKey("WorkflowDefinitionVersionId"))
            {
                await InvokeWithBlazorServiceContext(async () =>
                {
                    var parentNodeId = activity.GetNodeId();
                    var selectedActivityGraph = await WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, parentNodeId);
                    if (selectedActivityGraph == null) throw new InvalidOperationException($"Could not find selected activity graph for {parentNodeId}");
                    var selectedPortGraph = selectedActivityGraph.Children.FirstOrDefault(x => x.Port == portName);
                    if (selectedPortGraph == null) throw new InvalidOperationException($"Could not find selected activity graph for {portName}");

                    StitchNodesRecursive(selectedPortGraph);
                    embeddedActivity = selectedPortGraph.Activity;

                    if (embeddedActivity != null)
                        portProvider.AssignPort(portName, embeddedActivity, portProviderContext);
                });
            }
        }

        if (embeddedActivity != null)
        {
            var embeddedActivityTypeName = embeddedActivity.GetTypeName();

            // If the embedded activity has no designer support, then open it in the activity properties editor by raising the ActivitySelected event.
            if (embeddedActivityTypeName != "Elsa.Flowchart" && embeddedActivityTypeName != "Elsa.Workflow")
            {
                ActivitySelected?.Invoke(embeddedActivity);
                return;
            }

            // If the embedded activity type is a flowchart or workflow, we can display it in the designer.
        }
        else
        {
            if (!IsReadOnly)
            {
                // Create a flowchart and embed it into the activity.
                embeddedActivity = new JsonObject(new Dictionary<string, JsonNode?>
                {
                    ["id"] = IdentityGenerator.GenerateId(),
                    ["type"] = "Elsa.Flowchart",
                    ["version"] = 1,
                    ["name"] = "Flowchart1",
                });

                portProvider.AssignPort(args.PortName, embeddedActivity,
                    new PortProviderContext(activityDescriptor, activity));

                // Update the graph in the designer.
                await _diagramDesigner!.UpdateActivityAsync(activity.GetId(), activity);
            }
            else
            {
                return;
            }
        }

        // Create a new path segment of the container activity and push it onto the stack.
        var segment = new ActivityPathSegment(activity.GetId(), activity.GetTypeName(), args.PortName);

        await UpdatePathSegmentsAsync(segments => segments.Push(segment));
        await DisplayCurrentSegmentAsync();
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
            var portName = currentSegment.PortName;
            var activityTypeName = currentActivity.GetTypeName();
            var activityVersion = currentActivity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, currentActivity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);

            portProvider.AssignPort(portName, rootActivity, portProviderContext);
        }

        if (GraphUpdated != null)
            await GraphUpdated();
    }

    private async Task OnBreadcrumbItemClicked(BreadcrumbItem item)
    {
        if (item.Href == "#_root_")
        {
            await UpdatePathSegmentsAsync(segments => segments.Clear());
            await DisplayCurrentSegmentAsync();
            return;
        }

        var activityId = item.Href![1..];

        await UpdatePathSegmentsAsync(segments =>
        {
            while (segments.TryPeek(out var segment))
                if (segment.ActivityId == activityId)
                    break;
                else
                    segments.Pop();
        });

        await DisplayCurrentSegmentAsync();
    }
}