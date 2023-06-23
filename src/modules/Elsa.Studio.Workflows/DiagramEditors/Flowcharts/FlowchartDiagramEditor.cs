using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Designer.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramEditors.Flowcharts;

public class FlowchartDiagramEditor : IDiagramEditorToolboxProvider
{
    private FlowchartDesigner? _designer;

    public async Task UpdateActivityAsync(Activity activity)
    {
        await _designer!.UpdateActivityAsync(activity);
    }

    public async Task<Activity> ReadRootActivityAsync()
    {
        return await _designer!.ReadFlowchartAsync();
    }

    public RenderFragment Display(DisplayContext context)
    {
        var flowchart = (Flowchart)context.Activity;
        var sequence = 0;

        return builder =>
        {
            builder.OpenComponent<FlowchartDesigner>(sequence++);
            builder.AddAttribute(sequence++, nameof(FlowchartDesigner.Flowchart), flowchart);
            builder.AddAttribute(sequence++, nameof(FlowchartDesigner.OnActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesigner.OnGraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designer = (FlowchartDesigner)@ref);

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

    private async Task OnZoomToFitClicked() => await _designer!.ZoomToFitAsync();
    private async Task OnCenterClicked() => await _designer!.CenterContentAsync();

    private async Task OnAutoLayoutClicked()
    {
    }
}