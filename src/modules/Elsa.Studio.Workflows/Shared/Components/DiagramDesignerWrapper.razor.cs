using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

/// A wrapper around the diagram designer that provides a breadcrumb and a toolbar.
public partial class DiagramDesignerWrapper
{
    private IDiagramDesigner? _diagramDesigner;
    private Stack<ActivityPathSegment> _pathSegments = new();
    private List<BreadcrumbItem> _breadcrumbItems = new();
    private IDictionary<string, ActivityStats> _activityStats = new Dictionary<string, ActivityStats>();
    private ActivityGraph _activityGraph = default!;

    /// The workflow definition version ID.
    [Parameter] public string WorkflowDefinitionVersionId { get; set; } = default!;

    /// The root activity to display.
    [Parameter] public JsonObject Activity { get; set; } = default!;

    /// Whether the designer is read-only.
    [Parameter] public bool IsReadOnly { get; set; }

    /// The workflow instance ID, if any.
    [Parameter] public string? WorkflowInstanceId { get; set; }

    /// A custom toolbar to display.
    [Parameter] public RenderFragment? CustomToolbarItems { get; set; }

    /// Whether the designer is progressing.
    [Parameter] public bool IsProgressing { get; set; }

    /// An event raised when an activity is selected.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event raised when an embedded port is selected.
    [Parameter] public EventCallback GraphUpdated { get; set; }

    /// An event raised when the path changes.
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
    /// Selects the activity with the specified node ID.
    /// </summary>
    /// <param name="nodeId">The ID of the activity node to select.</param>
    public async Task SelectActivityAsync(string nodeId)
    {
        var containerActivity = GetCurrentContainerActivity();
        var activities = containerActivity.GetActivities();
        var activityToSelect = activities.FirstOrDefault(x => x.GetNodeId() == nodeId);

        if (activityToSelect != null)
        {
            await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
            return;
        }

        // The selected activity is not a direct child of the current container.
        // We need to find the container that owns the activity and update the path segments accordingly.

        // Lazy load the selected node path.
        var pathSegmentsResponse = await WorkflowDefinitionService.GetPathSegmentsAsync(WorkflowDefinitionVersionId, nodeId);

        if (pathSegmentsResponse == null)
            return;

        activityToSelect = pathSegmentsResponse.ChildNode.Activity;
        var containerNode = await ActivityVisitor.VisitAsync(pathSegmentsResponse.Container.Activity);
        _activityGraph.Merge(containerNode);
        StateHasChanged();

        // Reassign the current path.
        await UpdatePathSegmentsAsync(segments =>
        {
            segments.Clear();

            foreach (var segment in pathSegmentsResponse.PathSegments)
                segments.Push(segment);
        });

        // Display new segment.
        await DisplayCurrentSegmentAsync();

        // Select the activity.
        await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
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
    public async Task<JsonObject> ReadActivityAsync()
    {
        return await _diagramDesigner!.ReadRootActivityAsync();
    }

    /// <summary>
    /// Gets the root activity graph.
    /// </summary>
    public Task<ActivityGraph> GetActivityGraphAsync()
    {
        return Task.FromResult(_activityGraph);
    }

    /// <summary>
    /// Gets the root activity.
    /// </summary>
    public Task<JsonObject> GetActivityAsync()
    {
        var rootActivity = _activityGraph.Activity;
        return Task.FromResult(rootActivity);
    }

    /// <summary>
    /// Loads the specified activity into the designer.
    /// </summary>
    /// <param name="activity">The activity to load.</param>
    public async Task LoadActivityAsync(JsonObject activity)
    {
        Activity = activity;
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(Activity);
        _activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(Activity);
        await UpdatePathSegmentsAsync(segments => segments.Clear());
        StateHasChanged();
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
            if (GraphUpdated.HasDelegate)
                await GraphUpdated.InvokeAsync();

            return;
        }

        await _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    /// <summary>
    /// Updates the current path.
    /// </summary>
    /// <param name="action">A delegate that manipulates the path</param>
    public async Task UpdatePathSegmentsAsync(Action<Stack<ActivityPathSegment>> action)
    {
        action(_pathSegments);
        await UpdateBreadcrumbItemsAsync();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        await LoadActivityAsync(Activity);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
    }

    private JsonObject GetCurrentContainerActivity()
    {
        var lastSegment = _pathSegments.FirstOrDefault();

        if (lastSegment == null)
            return _activityGraph.Activity;

        var nodeId = lastSegment.ActivityNodeId;
        var node = _activityGraph.ActivityNodeLookup.TryGetValue(nodeId, out var nodeObject) ? nodeObject : null;

        if (node is null)
            return Activity;

        var activity = node.Activity;
        var port = lastSegment.PortName;
        var embeddedActivity = GetEmbeddedActivity(activity, port);

        if (embeddedActivity?.GetTypeName() == "Elsa.Workflow")
            embeddedActivity = embeddedActivity.GetRoot();

        return embeddedActivity ?? Activity;
    }

    private JsonObject? GetEmbeddedActivity(JsonObject activity, string portName)
    {
        var activityTypeName = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
        var portProviderContext = new PortProviderContext(activityDescriptor, activity);
        var portProvider = ActivityPortService.GetProvider(portProviderContext);
        var activityInPort = portProvider.ResolvePort(portName, portProviderContext);

        return activityInPort;
    }

    private async Task UpdateBreadcrumbItemsAsync()
    {
        _breadcrumbItems = (await GetBreadcrumbItems()).ToList();
        StateHasChanged();
    }

    private async Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbItems()
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        if (_pathSegments.Any())
            breadcrumbItems.Add(new BreadcrumbItem("Root", "#_root_", false, Icons.Material.Outlined.Home));

        var nodeLookup = _activityGraph.ActivityNodeLookup;
        var firstSegment = _pathSegments.FirstOrDefault();

        foreach (var segment in _pathSegments.Reverse())
        {
            var activityNodeId = segment.ActivityNodeId;

            if (!nodeLookup.TryGetValue(activityNodeId, out var activityNode))
            {
                activityNode = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, activityNodeId));
                var visitedActivityNode = await ActivityVisitor.VisitAsync(activityNode!.Activity);
                _activityGraph.Merge(visitedActivityNode);
                nodeLookup = _activityGraph.ActivityNodeLookup;
            }

            var activity = activityNode.Activity;
            var activityTypeName = activity.GetTypeName();
            var activityVersion = activity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, activity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);
            var ports = portProvider.GetPorts(portProviderContext);
            var embeddedPort = ports.First(x => x.Name == segment.PortName);
            var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityTypeName);
            var disabled = segment == firstSegment;
            var activityDisplayText = activity.GetName() ?? activityDescriptor.DisplayName;
            var breadcrumbDisplayText = $"{activityDisplayText}: {embeddedPort.DisplayName}";
            var activityBreadcrumbItem = new BreadcrumbItem(breadcrumbDisplayText, $"#{activity.GetId()}", disabled, displaySettings.Icon);

            breadcrumbItems.Add(activityBreadcrumbItem);
        }

        return breadcrumbItems;
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

    private async Task OnActivityEmbeddedPortSelected(ActivityEmbeddedPortSelectedArgs args)
    {
        var nodes = _activityGraph.ActivityNodeLookup;
        var selectedActivity = args.Activity;
        var activity = nodes.TryGetValue(selectedActivity.GetNodeId(), out var selectedActivityNode) ? selectedActivityNode.Activity : null;

        if (activity is null)
            return;

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
                    var selectedActivityGraph = await WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, parentNodeId) ?? throw new InvalidOperationException($"Could not find selected activity graph for {parentNodeId}");
                    var visitedActivityNode = await ActivityVisitor.VisitAsync(selectedActivityGraph.Activity);
                    _activityGraph.Merge(visitedActivityNode);

                    var propName = portName.Camelize();
                    var selectedPortActivity = (JsonObject)selectedActivityGraph.Activity[propName]!;
                    embeddedActivity = selectedPortActivity;
                });
            }
        }

        if (embeddedActivity != null)
        {
            var embeddedActivityTypeName = embeddedActivity.GetTypeName();

            // If the embedded activity has no designer support, then open it in the activity properties editor by raising the ActivitySelected event.
            if (embeddedActivityTypeName != "Elsa.Flowchart" && embeddedActivityTypeName != "Elsa.Workflow")
            {
                if (ActivitySelected.HasDelegate)
                    await ActivitySelected.InvokeAsync(embeddedActivity);
                return;
            }

            // If the embedded activity type is a flowchart or workflow, we can display it in the designer.
        }
        else
        {
            if (!IsReadOnly)
            {
                var embeddedActivityId = IdentityGenerator.GenerateId();
                // Create a flowchart and embed it into the activity.
                embeddedActivity = new JsonObject(new Dictionary<string, JsonNode?>
                {
                    ["id"] = embeddedActivityId,
                    ["nodeId"] = $"{activity.GetNodeId()}:{embeddedActivityId}",
                    ["type"] = "Elsa.Flowchart",
                    ["version"] = 1,
                    ["name"] = "Flowchart1",
                });

                portProvider.AssignPort(args.PortName, embeddedActivity, new PortProviderContext(activityDescriptor, activity));

                // Update the graph in the designer.
                await _diagramDesigner!.UpdateActivityAsync(activity.GetId(), activity);
            }
            else
            {
                return;
            }
        }

        // Create a new path segment of the container activity and push it onto the stack.
        var segment = new ActivityPathSegment(activity.GetNodeId(), activity.GetId(), activity.GetTypeName(), args.PortName);

        await UpdatePathSegmentsAsync(segments => segments.Push(segment));
        await DisplayCurrentSegmentAsync();
    }

    private async Task OnGraphUpdated()
    {
        var embeddedActivity = await _diagramDesigner!.ReadRootActivityAsync();
        var currentSegment = CurrentPathSegment;

        if (currentSegment == null) // Root activity was updated.
        {
            _activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(embeddedActivity);
        }
        else
        {
            var currentActivityNode = _activityGraph.ActivityNodeLookup[currentSegment.ActivityNodeId];
            var currentActivity = currentActivityNode.Activity;
            var portName = currentSegment.PortName;
            var activityTypeName = currentActivity.GetTypeName();
            var activityVersion = currentActivity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, currentActivity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);

            portProvider.AssignPort(portName, embeddedActivity, portProviderContext);
        }

        if (GraphUpdated.HasDelegate)
            await GraphUpdated.InvokeAsync();
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