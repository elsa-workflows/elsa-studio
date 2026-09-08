using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A Blazor component that renders the read-only BPMN designer: the X6 canvas that draws an
/// <c>Elsa.BpmnProcess</c> activity's diagram. Owns the JS interop to the BPMN adapter
/// (<c>src/designer/api/bpmn-designer.ts</c>), mirroring how <see cref="FlowchartDesigner"/> owns
/// the flowchart canvas's.
/// </summary>
public partial class BpmnDesigner : IAsyncDisposable
{
    private readonly string _containerId = $"bpmn-container-{Guid.NewGuid():N}";
    private DotNetObjectReference<BpmnDesigner>? _componentRef;
    private BpmnGraphApi? _graphApi;
    private JsonObject? _activity;
    private readonly PendingActionsQueue _pendingGraphActions;

    /// <inheritdoc />
    public BpmnDesigner()
    {
        _pendingGraphActions = new(() => new(_graphApi != null!), () => Logger);
    }

    /// The root <c>Elsa.BpmnProcess</c> activity to render.
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// The imported BPMN document's source XML, for BPMN DI geometry. Null when there is none, in
    /// which case the adapter's own fallback layout applies.
    [Parameter] public string? SourceXml { get; set; }

    /// The activity stats to render.
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// An event raised when an activity is selected -- the bound child activity for a bound element,
    /// or the scope's own <c>Elsa.BpmnProcess</c> activity otherwise.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event raised when an activity is double-clicked. Same mapping as <see cref="ActivitySelected"/>.
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }

    /// An event raised when the canvas, a lane or a pool is selected, i.e. nothing in particular.
    [Parameter] public EventCallback CanvasSelected { get; set; }

    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] private ILogger<BpmnDesigner> Logger { get; set; } = null!;

    /// <summary>
    /// Invoked from JavaScript when a BPMN element is selected.
    /// </summary>
    [JSInvokable]
    public async Task HandleActivitySelected(BpmnElementSelection selection)
    {
        if (!ActivitySelected.HasDelegate)
            return;

        var activity = ResolveSelectedActivity(selection);

        if (activity != null)
            await ActivitySelected.InvokeAsync(activity);
    }

    /// <summary>
    /// Invoked from JavaScript when a BPMN element is double-clicked.
    /// </summary>
    [JSInvokable]
    public async Task HandleActivityDoubleClick(BpmnElementSelection selection)
    {
        if (!ActivityDoubleClick.HasDelegate)
            return;

        var activity = ResolveSelectedActivity(selection);

        if (activity != null)
            await ActivityDoubleClick.InvokeAsync(activity);
    }

    /// <summary>
    /// Invoked from JavaScript when the canvas, a lane or a pool is selected.
    /// </summary>
    [JSInvokable]
    public async Task HandleCanvasSelected()
    {
        if (CanvasSelected.HasDelegate)
            await CanvasSelected.InvokeAsync();
    }

    /// <summary>
    /// Loads the specified root activity into the graph.
    /// </summary>
    /// <param name="activity">The root <c>Elsa.BpmnProcess</c> activity.</param>
    /// <param name="sourceXml">The imported BPMN document's source XML, or null.</param>
    /// <param name="activityStats">A map of activity stats.</param>
    public async Task LoadBpmnAsync(JsonObject activity, string? sourceXml, IDictionary<string, ActivityStats>? activityStats)
    {
        _activity = activity;
        SourceXml = sourceXml;
        ActivityStats = activityStats;

        await ActivityRegistry.EnsureLoadedAsync();

        var payload = new JsonObject
        {
            ["activity"] = activity.DeepClone(),
            ["sourceXml"] = sourceXml,
            ["activityDescriptors"] = BuildActivityDescriptors()
        };

        var diagnostics = await ScheduleGraphActionAsync(() => _graphApi!.LoadDiagramAsync(payload));

        foreach (var diagnostic in diagnostics.Where(d => d.Severity == "error"))
            Logger.LogWarning("BPMN diagram '{ActivityId}' reported an error: {Message}", activity.GetId(), diagnostic.Message);

        if (activityStats != null)
        {
            foreach (var (activityId, stats) in activityStats)
                await ScheduleGraphActionAsync(() => _graphApi!.UpdateActivityStatsAsync(activityId, stats));
        }
    }

    /// <summary>
    /// Updates the stats of the specified activity's bound elements.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="stats">The updated stats.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats) =>
        await ScheduleGraphActionAsync(() => _graphApi!.UpdateActivityStatsAsync(activityId, stats));

    /// <summary>
    /// Selects the element bound to the specified activity, through <c>workBindings</c>. A no-op for
    /// an id that names the root or a scope, since neither is bound work.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    public async Task SelectActivityAsync(string activityId)
    {
        if (_activity == null)
            return;

        var elementId = ResolveElementId(_activity, activityId);

        if (elementId != null)
            await ScheduleGraphActionAsync(() => _graphApi!.SelectElementAsync(elementId, true));
    }

    /// Zoom the canvas to fit all elements.
    public async Task ZoomToFitAsync() => await ScheduleGraphActionAsync(() => _graphApi!.ZoomToFitAsync());

    /// Center the canvas content.
    public async Task CenterContentAsync() => await ScheduleGraphActionAsync(() => _graphApi!.CenterContentAsync());

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            _graphApi = await DesignerJsInterop.CreateBpmnGraphAsync(_containerId, _componentRef);
            await _pendingGraphActions.ProcessAsync();
        }
    }

    private JsonNode? BuildActivityDescriptors()
    {
        var descriptors = ActivityRegistry.List().Select(d =>
        {
            var settings = ActivityDisplaySettingsRegistry.GetSettings(d.TypeName);
            return new ActivityDescriptorDto(d.TypeName, d.Version, d.Name, d.DisplayName ?? d.Name, d.Category, d.Description, settings.Color, settings.Icon);
        }).ToList();

        return JsonSerializer.SerializeToNode(descriptors, GetSerializerOptions());
    }

    private JsonObject? ResolveSelectedActivity(BpmnElementSelection selection)
    {
        if (_activity == null)
            return null;

        var targetId = selection.ActivityId ?? selection.ScopeActivityId;
        return FindActivityById(_activity, targetId);
    }

    /// <summary>
    /// Finds the activity with the specified id, in <paramref name="root"/> itself or, recursively,
    /// among the activities bound to it and to any nested BPMN scope. Used to resolve both a bound
    /// element's activity and a scope's own <c>Elsa.BpmnProcess</c> activity by the same lookup.
    /// </summary>
    internal static JsonObject? FindActivityById(JsonObject root, string id)
    {
        if (root.GetId() == id)
            return root;

        foreach (var activity in root.GetActivities())
        {
            var found = FindActivityById(activity, id);

            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Maps an Elsa activity id back to the BPMN element bound to it, through the owning scope's
    /// <c>workBindings</c> and its process definition's elements. Returns null for an id that is not
    /// bound work -- the root, a scope, or an id this document does not carry -- so the caller can
    /// treat that as a no-op.
    /// </summary>
    internal static string? ResolveElementId(JsonObject scope, string activityId)
    {
        if (scope["workBindings"] is JsonObject workBindings)
        {
            var bindingRef = workBindings.FirstOrDefault(binding => binding.Value?.GetValue<string>() == activityId).Key;

            if (bindingRef != null &&
                scope["process"] is JsonObject process &&
                process["elements"] is JsonArray elements)
            {
                var element = elements.OfType<JsonObject>().FirstOrDefault(e => e["bindingRef"]?.GetValue<string>() == bindingRef);

                if (element != null)
                    return element["elementId"]?.GetValue<string>();
            }
        }

        foreach (var activity in scope.GetActivities())
        {
            var found = ResolveElementId(activity, activityId);

            if (found != null)
                return found;
        }

        return null;
    }

    private async Task ScheduleGraphActionAsync(Func<Task> action) => await _pendingGraphActions.EnqueueAsync(action);
    private async Task<T> ScheduleGraphActionAsync<T>(Func<Task<T>> action) => await _pendingGraphActions.EnqueueAsync(action);

    private static JsonSerializerOptions GetSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_graphApi != null)
            await _graphApi.DisposeGraphAsync();

        _componentRef?.Dispose();
    }
}
