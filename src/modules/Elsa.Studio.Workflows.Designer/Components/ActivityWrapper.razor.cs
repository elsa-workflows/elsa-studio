using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A wrapper for an activity component.
/// </summary>
public partial class ActivityWrapper
{
    private string _label = default!;
    private string _description = default!;
    private bool _showDescription;
    private string _color = default!;
    private string? _icon;
    private ActivityDescriptor _activityDescriptor = default!;
    private ICollection<Port> _ports = new List<Port>();

    /// <summary>
    /// Gets or sets the element ID.
    /// </summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    [Parameter]
    public string ActivityId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the activity.
    /// </summary>
    [Parameter]
    public JsonObject Activity { get; set; } = default!;

    /// <summary>
    /// Until the max depth of JSInterop is configurable to exceed 32, we need to pass the activity JSON as a string.
    /// </summary>
    [Parameter]
    public string ActivityJson { get; set; } = default!;

    /// <summary>
    /// Gets or sets the selected port name.
    /// </summary>
    [Parameter]
    public string? SelectedPortName { get; set; }

    /// <summary>
    /// Gets or sets the activity stats.
    /// </summary>
    [Parameter]
    public ActivityStats? Stats { get; set; }

    [Inject] private DesignerJsInterop DesignerInterop { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IActivityPortService ActivityPortService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ActivityJson))
            Activity = JsonSerializer.Deserialize<JsonObject>(ActivityJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        await ActivityRegistry.EnsureLoadedAsync();

        var activity = Activity;
        var activityDisplayText = activity.GetDisplayText()?.Trim();
        var activityDescription = activity.GetDescription()?.Trim();
        var activityType = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var descriptor = ActivityRegistry.Find(activityType, activityVersion);
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        _label = !string.IsNullOrEmpty(activityDisplayText) ? activityDisplayText : descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";
        _description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor?.Description ?? string.Empty;
        _showDescription = activity.GetShowDescription() == true;
        _color = displaySettings.Color;
        _icon = displaySettings.Icon;
        _activityDescriptor = descriptor!;
        _ports = ActivityPortService.GetPorts(new PortProviderContext(descriptor!, activity)).ToList();

        await UpdateSizeAsync();
    }

    private async Task UpdateSizeAsync()
    {
        if (!string.IsNullOrEmpty(ElementId))
        {
            var size = Activity.GetDesignerMetadata().Size;
            await DesignerInterop.UpdateActivitySizeAsync(ElementId, Activity, size);
        }
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

    private async Task OnDeleteEmbeddedActivityClicked(string portName)
    {
        var providerContext = new PortProviderContext(_activityDescriptor, Activity);
        var portProvider = ActivityPortService.GetProvider(providerContext);
        portProvider.ClearPort(portName, providerContext);
        await UpdateSizeAsync();
    }
}