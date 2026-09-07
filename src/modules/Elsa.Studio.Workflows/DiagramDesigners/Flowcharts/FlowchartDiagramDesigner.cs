using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer that displays a flowchart.
/// </summary>
public class FlowchartDiagramDesigner(ILocalizer localizer, IDialogService dialogService, IOptions<DesignerOptions> designerOptions) : IDiagramDesignerToolboxProvider
{
    private const string DefaultFileName = "flowchart";

    private FlowchartDesignerWrapper? _designerWrapper;
    private readonly Guid _id = Guid.NewGuid();

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
    public async Task<JsonObject> ReadRootActivityAsync()
    {
        return await _designerWrapper!.ReadRootActivityAsync();
    }

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
        yield return DisplayToolboxItem(localizer["Auto layout"], Icons.Material.Outlined.AutoAwesomeMosaic, localizer["Auto layout"], OnAutoLayoutClicked);

        // Exporting is available in both read-only and editable mode, but only the X6 designer can produce an image.
        if (!designerOptions.Value.UseReactFlow)
            yield return DisplayToolboxItem(localizer["Export"], Icons.Material.Outlined.Download, localizer["Export as image"], OnExportClicked);
    }

    private RenderFragment DisplayToolboxItem(string title, string icon, string description, Func<Task> onClick)
    {
        return builder =>
        {
            builder.OpenComponent<MudTooltip>(0);
            builder.AddAttribute(1, nameof(MudTooltip.Text), description);
            builder.AddAttribute(2, nameof(MudTooltip.Delay), 500d);
            builder.AddAttribute(3, nameof(MudTooltip.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MudIconButton>(0);
                childBuilder.AddAttribute(1, nameof(MudIconButton.Icon), icon);
                childBuilder.AddAttribute(2, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
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

    private async Task OnExportClicked()
    {
        if (_designerWrapper == null)
            return;

        var parameters = new DialogParameters<ExportFlowchartDialog>
        {
            { x => x.FileName, GetDefaultFileName(_designerWrapper.Flowchart) }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<ExportFlowchartDialog>(localizer["Export flowchart"], parameters, options);
        var result = await dialog.Result;

        if (result?.Canceled != false || result.Data is not ExportGraphOptions exportOptions)
            return;

        await _designerWrapper.ExportGraphAsync(exportOptions);
    }

    /// <summary>
    /// Derives the file name to propose for an export from the flowchart's name, suffixed with its version when the
    /// flowchart carries one. Characters that the host platform does not allow in a file name are replaced with an
    /// underscore; the browser applies its own sanitization to the download name on top of this.
    /// </summary>
    internal static string GetDefaultFileName(JsonObject? flowchart)
    {
        var name = flowchart?.GetName()?.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var fileName = string.IsNullOrEmpty(name)
            ? DefaultFileName
            : new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        var version = flowchart?.GetVersion() ?? 0;

        return version > 0 ? $"{fileName}_v{version}" : fileName;
    }
}