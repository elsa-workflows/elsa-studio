using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer that displays a flowchart.
/// </summary>
public class FlowchartDiagramDesigner(ILocalizer localizer, IDialogService dialogService) : IDiagramDesignerToolboxProvider
{
    private FlowchartDesignerWrapper? _designerWrapper;
    private readonly Guid _id = Guid.NewGuid();

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => await InvokeDesignerActionAsync(x => x.LoadFlowchartAsync(activity, activityStatsMap));

    /// <inheritdoc />
    public async Task UpdateActivityAsync(string id, JsonObject activity) => await InvokeDesignerActionAsync(x => x.UpdateActivityAsync(id, activity));

    /// <inheritdoc />
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats) => await InvokeDesignerActionAsync(x => x.UpdateActivityStatsAsync(id, stats));
    
    /// <inheritdoc />
    public async Task SelectActivityAsync(string id) => await InvokeDesignerActionAsync(x => x.SelectActivityAsync(id));

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
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.Flowchart),  flowchart);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityUpdated), context.ActivityUpdated);
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
        yield return DisplayToolboxItem(localizer["Zoom to fit"], Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DisplayToolboxItem(localizer["Center"], Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);

        if (!isReadonly)
        {
            yield return DisplayToolboxItem(localizer["Auto layout"], Icons.Material.Outlined.AutoAwesomeMosaic, localizer["Auto layout"], OnAutoLayoutClicked);
        }

        yield return DisplayToolboxItem(localizer["Capture"], @Icons.Material.Outlined.Fullscreen, localizer["Capture flowchart"], OnCaptureClicked);
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
                childBuilder.AddAttribute(2, "title", title);
                childBuilder.AddAttribute(3, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
                childBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private async Task InvokeDesignerActionAsync(Func<FlowchartDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null && action != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper!.CenterContentAsync() : Task.CompletedTask;
    private Task OnAutoLayoutClicked() => _designerWrapper != null ? _designerWrapper!.AutoLayoutAsync() : Task.CompletedTask;
    private async Task OnCaptureClicked()
    {
        if (_designerWrapper is null) return;

        var flowchart = _designerWrapper.Flowchart;
        var invalidChars = Path.GetInvalidFileNameChars();
        var name = flowchart?.GetName() ?? "Workflow";
        var version = flowchart?.GetVersion();
        var validFileName = string.Concat(name.Select(c => invalidChars.Contains(c) ? "_" : c.ToString())).Trim()
                            + (version is null ? string.Empty : $"_v{version}");

        var parameters = new DialogParameters<CaptureFlowchartDialog>
            {
                { x => x.FileName, validFileName },
            };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await dialogService.ShowAsync<CaptureFlowchartDialog>(localizer["Capture content"], parameters, options);
        var dialogResult = await dialogInstance.Result;
        if (dialogResult?.Canceled ?? true) return;

        var captureOptions = dialogResult?.Data as CaptureOptions ?? new();
        await _designerWrapper.ExportContentToFormatAsync(captureOptions);
    }
}