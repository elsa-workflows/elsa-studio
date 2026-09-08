using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A diagram designer that displays an imported BPMN process, read-only. Editing lands in a later
/// increment (W14); this designer only ever shows the diagram and forwards selection.
/// </summary>
public class BpmnDiagramDesigner(ILocalizer localizer, IOptions<DesignerOptions> designerOptions) : IDiagramDesignerToolboxProvider
{
    /// <summary>
    /// The custom property key elsa-core stores the imported BPMN document's source XML under.
    /// </summary>
    private const string SourceXmlCustomPropertyKey = "Bpmn:SourceXml";

    private readonly Guid _id = Guid.NewGuid();
    private BpmnDesignerWrapper? _designerWrapper;
    private JsonObject _rootActivity = [];
    private string? _sourceXml;

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        _rootActivity = activity;
        await InvokeDesignerActionAsync(x => x.LoadBpmnAsync(activity, _sourceXml, activityStatsMap));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The canvas is read-only and has nothing to react to, so this keeps two copies of the in-memory
    /// root activity in step: the matching child activity node is replaced by id, wherever in the tree
    /// it is, so that <see cref="ReadRootActivityAsync"/> returns the edited tree, and the same
    /// replacement is forwarded to the mounted <see cref="BpmnDesignerWrapper"/> so its own held tree
    /// -- the one selection resolves from -- does not go stale. The canvas itself is left untouched.
    /// </remarks>
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (_rootActivity.GetId() == id)
            _rootActivity = (JsonObject)activity.DeepClone()!;
        else
            ReplaceActivity(_rootActivity, id, activity);

        await InvokeDesignerActionAsync(x => x.UpdateActivityAsync(id, activity));
    }

    /// <inheritdoc />
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        await InvokeDesignerActionAsync(x => x.UpdateActivityStatsAsync(id, stats));
    }

    /// <inheritdoc />
    public async Task SelectActivityAsync(string id)
    {
        await InvokeDesignerActionAsync(x => x.SelectActivityAsync(id));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the whole root activity JSON exactly as it was given and since edited by
    /// <see cref="UpdateActivityAsync"/>, never a projection rebuilt from the read-only view model:
    /// the view model is the canvas's own concern and the document it was built from is what this
    /// designer holds and hands back (D4).
    /// </remarks>
    public Task<JsonObject> ReadRootActivityAsync() => Task.FromResult(_rootActivity);

    /// <inheritdoc />
    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        var activity = context.Activity;
        var sequence = 0;

        _rootActivity = activity;
        _sourceXml = GetSourceXml(context.WorkflowDefinition);

        return builder =>
        {
            builder.OpenComponent<BpmnDesignerWrapper>(sequence++);
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.Activity), activity);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.SourceXml), _sourceXml);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (BpmnDesignerWrapper)@ref);

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadOnly)
    {
        // The React Flow adapter for BPMN does not exist yet (W9b), so there is no canvas to zoom or
        // center while it is in effect.
        if (designerOptions.Value.UseReactFlow)
            yield break;

        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Zoom to fit"], Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Center"], Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);
    }

    private async Task InvokeDesignerActionAsync(Func<BpmnDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper.CenterContentAsync() : Task.CompletedTask;

    /// <summary>
    /// Replaces the activity with the specified id, wherever in the tree it is -- the root's own
    /// activities or, recursively, a nested BPMN scope's -- with a clone of <paramref name="replacement"/>.
    /// </summary>
    private static bool ReplaceActivity(JsonObject scope, string id, JsonObject replacement)
    {
        if (scope["activities"] is not JsonArray activities)
            return false;

        for (var i = 0; i < activities.Count; i++)
        {
            if (activities[i] is not JsonObject child)
                continue;

            if (child.GetId() == id)
            {
                activities[i] = (JsonObject)replacement.DeepClone()!;
                return true;
            }

            if (ReplaceActivity(child, id, replacement))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the imported BPMN document's source XML from the workflow definition's custom
    /// properties, or null when there is none.
    /// </summary>
    private static string? GetSourceXml(WorkflowDefinition? workflowDefinition)
    {
        if (workflowDefinition == null || !workflowDefinition.CustomProperties.TryGetValue(SourceXmlCustomPropertyKey, out var value) || value == null!)
            return null;

        return value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };
    }
}
