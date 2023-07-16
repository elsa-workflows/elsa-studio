using System.Diagnostics;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.Designer.Components;

public partial class ActivityWrapper
{
    private string _label = default!;
    private string _description = default!;
    private bool _showDescription;
    private string _color = default!;
    private string? _icon;
    private ActivityDescriptor _activityDescriptor = default!;

    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = default!;
    [Parameter] public JsonObject Activity { get; set; } = default!;
    [Parameter] public string? SelectedPortName { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = default!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;

    protected override async Task OnInitializedAsync()
    {
        var activity = Activity;
        var activityDisplayText = activity.GetDisplayText()?.Trim();
        var activityDescription = activity.GetDescription()?.Trim();
        var activityType = activity.GetTypeName();
        var descriptor = await ActivityRegistry.FindAsync(activityType);
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        _label = !string.IsNullOrEmpty(activityDisplayText) ? activityDisplayText : descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";
        _description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor?.Description ?? string.Empty;
        _showDescription = activity.GetShowDescription() == true;
        _color = displaySettings.Color;
        _icon = displaySettings.Icon;
        _activityDescriptor = descriptor!;

        await UpdateSizeAsync();
    }

    private async Task UpdateSizeAsync()
    {
        // If the activity has a size specified, don't attempt to calculate it.
        var size = Activity.GetDesignerMetadata().Size;

        if (size.Width > 0 || size.Height > 0)
            return;

            // Otherwise, update the activity node.
        if (!string.IsNullOrEmpty(ElementId))
            await DesignerInterop.UpdateActivitySizeAsync(ElementId, Activity);
    }

    private async Task OnEmbeddedActivityClicked(JsonObject childActivity)
    {
        var elementId = $"activity-{childActivity.GetId()}";
        await DesignerInterop.RaiseActivitySelectedAsync(elementId, childActivity);
    }

    private async Task OnEmptyPortClicked(Port port)
    {
        var activity = Activity;
        var elementId = $"activity-{activity.GetId()}";
        await DesignerInterop.RaiseActivityEmbeddedPortSelectedAsync(elementId, activity, port.Name);
    }

    private Task OnChildActivityDragStart()
    {
        return Task.CompletedTask;
    }
}