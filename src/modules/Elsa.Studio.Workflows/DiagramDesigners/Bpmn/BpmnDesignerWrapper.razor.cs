using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A wrapper around the <see cref="Designer.Components.BpmnDesigner"/> component that switches
/// between the X6 canvas and, while <see cref="DesignerOptions.UseReactFlow"/> is in effect, a notice
/// that BPMN is not yet available on the React Flow canvas -- exactly as <c>FlowchartDesignerWrapper</c>
/// switches, except that dropping W9b's React Flow adapter here is a clear notice, not the JSON
/// fallback and not a crash.
/// </summary>
public partial class BpmnDesignerWrapper
{
    /// <summary>
    /// The root <c>Elsa.BpmnProcess</c> activity to display.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// <summary>
    /// The imported BPMN document's source XML, for BPMN DI geometry.
    /// </summary>
    [Parameter] public string? SourceXml { get; set; }

    /// <summary>
    /// A map of activity stats.
    /// </summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// <summary>
    /// An event raised when an activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// <summary>
    /// An event raised when an activity is double-clicked.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }

    [Inject] private IOptions<DesignerOptions> DesignerOptions { get; set; } = null!;

    private BpmnDesigner? Designer { get; set; }
    private bool UseReactFlow => DesignerOptions.Value.UseReactFlow;

    /// <summary>
    /// Loads the specified root activity into the designer.
    /// </summary>
    public async Task LoadBpmnAsync(JsonObject activity, string? sourceXml, IDictionary<string, ActivityStats>? activityStats)
    {
        Activity = activity;
        SourceXml = sourceXml;
        ActivityStats = activityStats;

        if (Designer != null)
            await Designer.LoadBpmnAsync(activity, sourceXml, activityStats);
    }

    /// <summary>
    /// Updates the stats of the specified activity.
    /// </summary>
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        if (Designer != null)
            await Designer.UpdateActivityStatsAsync(id, stats);
    }

    /// <summary>
    /// Selects the element bound to the specified activity.
    /// </summary>
    public async Task SelectActivityAsync(string id)
    {
        if (Designer != null)
            await Designer.SelectActivityAsync(id);
    }

    /// <summary>
    /// Zooms the designer to fit the content.
    /// </summary>
    public async Task ZoomToFitAsync()
    {
        if (Designer != null)
            await Designer.ZoomToFitAsync();
    }

    /// <summary>
    /// Centers the content of the designer.
    /// </summary>
    public async Task CenterContentAsync()
    {
        if (Designer != null)
            await Designer.CenterContentAsync();
    }

    private async Task OnCanvasSelected()
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(Activity);
    }
}
