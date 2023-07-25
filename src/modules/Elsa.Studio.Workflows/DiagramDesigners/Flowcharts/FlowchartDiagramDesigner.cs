using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

public class FlowchartDiagramDesigner : IDiagramDesignerToolboxProvider
{
    private FlowchartDesignerWrapper? _designerWrapper;

    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => await _designerWrapper!.LoadFlowchartAsync(activity, activityStatsMap);
    public async Task UpdateActivityAsync(string id, JsonObject activity) => await _designerWrapper!.UpdateActivityAsync(id, activity);
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats) => await _designerWrapper!.UpdateActivityStatsAsync(id, stats); 

    public async Task<JsonObject> ReadRootActivityAsync() => await _designerWrapper!.ReadRootActivityAsync();

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
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (FlowchartDesignerWrapper)@ref);

            builder.CloseComponent();
        };
    }

    public IEnumerable<RenderFragment> GetToolboxItems()
    {
        yield return DisplayToolboxItem("Zoom to fit", Icons.Material.Outlined.FitScreen, "Zoom to fit the screen", OnZoomToFitClicked);
        yield return DisplayToolboxItem("Center", Icons.Material.Filled.FilterCenterFocus, "Center", OnCenterClicked);
        yield return DisplayToolboxItem("Auto layout", Icons.Material.Outlined.AutoAwesomeMosaic, "Auto layout", OnAutoLayoutClicked);
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

    private async Task OnZoomToFitClicked() => await _designerWrapper!.ZoomToFitAsync();
    private async Task OnCenterClicked() => await _designerWrapper!.CenterContentAsync();

    private async Task OnAutoLayoutClicked()
    {
    }
}