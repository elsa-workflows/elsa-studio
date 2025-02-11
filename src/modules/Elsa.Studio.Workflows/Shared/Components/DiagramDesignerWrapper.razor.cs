using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Contexts;
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
    private JsonObject? _currentContainerActivity;
    private List<BreadcrumbItem> _breadcrumbItems = new();
    private IDictionary<string, ActivityStats> _activityStats = new Dictionary<string, ActivityStats>();
    private ActivityGraph _activityGraph = null!;
    private IDictionary<string, ActivityNode> _indexedActivityNodes = new Dictionary<string, ActivityNode>();

    /// The workflow definition version ID.
    [Parameter] public string WorkflowDefinitionVersionId { get; set; } = null!;

    /// The root activity to display.
    [Parameter] public JsonObject Activity { get; set; } = null!;

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

    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] private IActivityPortService ActivityPortService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private ActivityPathSegment? CurrentPathSegment => _pathSegments.TryPeek(out var segment) ? segment : null;

    /// Selects the activity with the specified ID.
    /// <param name="activityId">The ID of the activity ID select.</param>
    public async Task SelectActivityByActivityIdAsync(string activityId)
    {
        var containerActivity = GetCurrentContainerActivity();
        var activities = containerActivity.GetActivities();
        var activityToSelect = activities.FirstOrDefault(x => x.GetId() == activityId);

        await SelectActivityAsync(activityToSelect);
    }

    /// Selects the activity with the specified node ID.
    /// <param name="nodeId">The ID of the activity node to select.</param>
    public async Task SelectActivityAsync(string nodeId)
    {
        var containerActivity = GetCurrentContainerActivityOrRoot();
        var activities = containerActivity.GetActivities();
        var activityToSelect = activities.FirstOrDefault(x => x.GetNodeId() == nodeId);

        await SelectActivityAsync(activityToSelect);
    }

    private async Task SelectActivityAsync(JsonObject? activityToSelect)
    {
        if (activityToSelect != null)
        {
            await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
            return;
        }

        // Load the selected node path from the backend.
        var pathSegmentsResponse = await WorkflowDefinitionService.GetPathSegmentsAsync(WorkflowDefinitionVersionId, activityToSelect!.GetNodeId());

        if (pathSegmentsResponse == null)
            return;

        activityToSelect = pathSegmentsResponse.ChildNode.Activity;
        var pathSegments = pathSegmentsResponse.PathSegments.ToList();
        StateHasChanged();

        // Reassign the current path.
        await UpdatePathSegmentsAsync(segments =>
        {
            segments.Clear();

            foreach (var segment in pathSegments)
                segments.Push(segment);
        });

        // Display the new segment.
        _currentContainerActivity = null;
        await DisplayCurrentSegmentAsync();

        // Select the activity.
        await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
    }

    /// Updates the stats of the specified activity.
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="stats">The stats to update.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        await _diagramDesigner!.UpdateActivityStatsAsync(activityId, stats);
    }

    /// Reads the activity from the designer.
    public async Task<JsonObject> ReadActivityAsync()
    {
        return await _diagramDesigner!.ReadRootActivityAsync();
    }

    /// Gets the root activity graph.
    public Task<ActivityGraph> GetActivityGraphAsync()
    {
        return Task.FromResult(_activityGraph);
    }

    /// Gets the root activity.
    public Task<JsonObject> GetActivityAsync()
    {
        var rootActivity = _activityGraph.Activity;
        return Task.FromResult(rootActivity);
    }

    /// Loads the specified activity into the designer.
    /// <param name="activity">The activity to load.</param>
    public async Task LoadActivityAsync(JsonObject activity)
    {
        Activity = activity;
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(activity);
        _activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(activity);
        await IndexActivityNodes(_activityGraph.Activity);
        await UpdatePathSegmentsAsync(segments => segments.Clear());
        await UpdateBreadcrumbItemsAsync();
        StateHasChanged();
    }

    /// Updates the specified activity in the designer.
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    public async Task UpdateActivityAsync(string activityId, JsonObject activity)
    {
        var currentContainer = GetCurrentContainerActivityOrRoot();

        if (currentContainer == activity)
        {
            if (GraphUpdated.HasDelegate)
                await GraphUpdated.InvokeAsync();

            return;
        }

        await _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    /// The current container activity or the root activity of the graph.
    public JsonObject GetCurrentContainerActivityOrRoot()
    {
        var activity = GetCurrentContainerActivity();
        return _currentContainerActivity = activity ?? Activity;
    }

    private async Task<JsonObject> GetCurrentContainerActivityOrRootAsync()
    {
        var activity = GetCurrentContainerActivity();

        if (activity == null)
        {
            var lastSegment = _pathSegments.First();
            var parentNodeId = lastSegment.ActivityNodeId;
            var selectedActivityGraph = await WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, parentNodeId);
            var propName = lastSegment.PortName.Camelize();
            var selectedPortActivity = (JsonObject?)selectedActivityGraph!.Activity[propName];
            activity = selectedPortActivity;
            _currentContainerActivity = activity;
            await IndexActivityNodes(selectedActivityGraph.Activity);
        }

        return activity ?? Activity;
    }

    private async Task IndexActivityNodes(JsonObject activity)
    {
        var visitedNode = await ActivityVisitor.VisitAsync(activity);
        var nodes = visitedNode.Flatten();
        foreach (var node in nodes) _indexedActivityNodes[node.NodeId] = node;
    }

    private JsonObject? GetCurrentContainerActivity()
    {
        if (_currentContainerActivity != null)
            return _currentContainerActivity;

        var lastSegment = _pathSegments.FirstOrDefault();

        if (lastSegment == null)
        {
            _currentContainerActivity = Activity;
            return _currentContainerActivity;
        }

        var nodeId = lastSegment.ActivityNodeId;
        var node = _indexedActivityNodes.TryGetValue(nodeId, out var activityNode) ? activityNode : null;

        if (node is null)
            return null;

        var activity = node.Activity;
        var port = lastSegment.PortName;
        var embeddedActivity = GetEmbeddedActivity(activity, port);

        if (embeddedActivity?.GetTypeName() == "Elsa.Workflow")
            embeddedActivity = embeddedActivity.GetRoot();

        return embeddedActivity ?? Activity;
    }

    /// The parent activity of the current activity being loaded in the designer.
    public JsonObject GetParentActivity()
    {
        var lastSegment = _pathSegments.FirstOrDefault();
        var nodeId = lastSegment?.ActivityNodeId;
        var node = nodeId != null ? _indexedActivityNodes.TryGetValue(nodeId, out var activityNode) ? activityNode : null : null;
        return node?.Activity ?? Activity;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        await LoadActivityAsync(Activity);
    }

    /// Updates the current path.
    /// <param name="action">A delegate that manipulates the path</param>
    private async Task UpdatePathSegmentsAsync(Action<Stack<ActivityPathSegment>> action)
    {
        action(_pathSegments);
        _currentContainerActivity = null;

        if (PathChanged.HasDelegate)
        {
            var parentActivity = GetParentActivity();
            var currentContainerActivity = GetCurrentContainerActivityOrRoot();
            await PathChanged.InvokeAsync(new DesignerPathChangedArgs(parentActivity, currentContainerActivity));
        }
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
        await RefreshActivityStatsAsync();
        StateHasChanged();
    }

    private async Task RefreshActivityStatsAsync()
    {
        if (WorkflowInstanceId != null)
        {
            var currentContainerActivity = GetCurrentContainerActivityOrRoot();
            var report = await ActivityExecutionService.GetReportAsync(WorkflowInstanceId, currentContainerActivity);
            _activityStats = report.Stats.ToDictionary(x => x.ActivityNodeId, x => new ActivityStats
            {
                Faulted = x.IsFaulted,
                Blocked = x.IsBlocked,
                Completed = x.CompletedCount,
                Started = x.StartedCount,
                Uncompleted = x.UncompletedCount
            });
        }
    }

    private async Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbItems()
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        if (_pathSegments.Any())
            breadcrumbItems.Add(new BreadcrumbItem("Root", "#_root_", false, Icons.Material.Outlined.Home));

        var nodeLookup = _indexedActivityNodes;
        var firstSegment = _pathSegments.FirstOrDefault();

        foreach (var segment in _pathSegments.Reverse())
        {
            var activityNodeId = segment.ActivityNodeId;

            if (!nodeLookup.TryGetValue(activityNodeId, out var activityNode))
            {
                activityNode = await WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, activityNodeId);
            }

            if (activityNode == null)
                continue;

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
        var currentContainerActivity = await GetCurrentContainerActivityOrRootAsync();

        if (_diagramDesigner == null)
            return;

        await _diagramDesigner.LoadRootActivityAsync(currentContainerActivity, _activityStats);
        await UpdateBreadcrumbItemsAsync();
    }

    private RenderFragment? DisplayDesigner()
    {
        return _diagramDesigner?.DisplayDesigner(new DisplayContext(
            GetCurrentContainerActivityOrRoot(),
            ActivitySelected,
            EventCallback.Factory.Create<ActivityEmbeddedPortSelectedArgs>(this, OnActivityEmbeddedPortSelected),
            EventCallback.Factory.Create<JsonObject>(this, OnActivityDoubleClick),
            EventCallback.Factory.Create(this, OnGraphUpdated),
            IsReadOnly,
            _activityStats));
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
        var nodes = _indexedActivityNodes;
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
                var parentNodeId = activity.GetNodeId();
                var selectedActivityGraph = await WorkflowDefinitionService.FindSubgraphAsync(WorkflowDefinitionVersionId, parentNodeId) ?? throw new InvalidOperationException($"Could not find selected activity graph for {parentNodeId}");
                var propName = portName.Camelize();
                var selectedPortActivity = (JsonObject)selectedActivityGraph.Activity[propName]!;
                embeddedActivity = selectedPortActivity;
                portProvider.AssignPort(args.PortName, embeddedActivity, new PortProviderContext(activityDescriptor, activity));
                await IndexActivityNodes(selectedActivityGraph.Activity);
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
            await IndexActivityNodes(_activityGraph.Activity);
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
            await _activityGraph.IndexAsync();
            await IndexActivityNodes(_activityGraph.Activity);
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

        _currentContainerActivity = null;
        await DisplayCurrentSegmentAsync();
    }
}