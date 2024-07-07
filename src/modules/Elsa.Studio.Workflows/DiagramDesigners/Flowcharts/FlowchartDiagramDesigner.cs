using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer that displays a flowchart.
/// </summary>
public class FlowchartDiagramDesigner : IDiagramDesignerToolboxProvider
{
    private FlowchartDesignerWrapper? _designerWrapper;

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        await InvokeDesignerActionAsync(x => x.LoadFlowchartAsync(activity, activityStatsMap));
    }

    /// <inheritdoc />
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
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
    public async Task<JsonObject> ReadRootActivityAsync() => await _designerWrapper!.ReadRootActivityAsync();

    /// <inheritdoc />
    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        var flowchart = context.Activity;
        var sequence = 0;

        return builder =>
        {
            builder.OpenComponent<FlowchartDesignerWrapper>(sequence++);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.Flowchart), flowchart);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (FlowchartDesignerWrapper)@ref);

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadonly)
    {
        yield return DisplayToolboxItem("Zoom to fit", Icons.Material.Outlined.FitScreen, "Zoom to fit the screen", OnZoomToFitClicked);
        yield return DisplayToolboxItem("Center", Icons.Material.Filled.FilterCenterFocus, "Center", OnCenterClicked);

        if (!isReadonly)
        {
            yield return DisplayToolboxItem("Auto layout", Icons.Material.Outlined.AutoAwesomeMosaic, "Auto layout",
                OnAutoLayoutClicked);
        }
    }

    private RenderFragment DisplayToolboxItem(string title, string icon, string description, Func<Task> onClick)
    {
        return builder =>
        {
            builder.OpenComponent<MudTooltip>(0);
            builder.AddAttribute(1, nameof(MudTooltip.Text), description);
            builder.AddAttribute(2, nameof(MudTooltip.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MudIconButton>(0);
                childBuilder.AddAttribute(1, nameof(MudIconButton.Icon), icon);
                childBuilder.AddAttribute(2, nameof(MudIconButton.Title), title);
                childBuilder.AddAttribute(3, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
                childBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private async Task InvokeDesignerActionAsync(Func<FlowchartDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null) await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper!.CenterContentAsync() : Task.CompletedTask;
    private Task OnAutoLayoutClicked() => _designerWrapper != null ? _designerWrapper!.AutoLayoutAsync() : Task.CompletedTask;
}