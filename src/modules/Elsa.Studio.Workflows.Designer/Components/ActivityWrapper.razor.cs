using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A wrapper for an activity component.
/// </summary>
public partial class ActivityWrapper
{
    private string _label = null!;
    private string _typeName = null!;
    private string _description = null!;
    private bool _showDescription;
    private string _color = null!;
    private string? _icon;
    private ActivityDescriptor _activityDescriptor = null!;
    private ICollection<Port> _ports = new List<Port>();

    /// <summary>
    /// Gets or sets the element ID.
    /// </summary>
    [Parameter] public string? ElementId { get; set; }

    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    [Parameter] public string ActivityId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// <summary>
    /// Until the max depth of JSInterop is configurable to exceed 32, we need to pass the activity JSON as a string.
    /// </summary>
    [Parameter] public string ActivityJson { get; set; } = null!;

    /// <summary>
    /// Gets or sets the selected port name.
    /// </summary>
    [Parameter] public string? SelectedPortName { get; set; }

    /// <summary>
    /// Gets or sets the activity stats.
    /// </summary>
    [Parameter] public ActivityStats? Stats { get; set; }

    [Inject] private DesignerJsInterop DesignerInterop { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] private IActivityPortService ActivityPortService { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;

    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;
    public ElementReference ActivatorRef { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ActivityJson))
            Activity = JsonSerializer.Deserialize<JsonObject>(ActivityJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        await ActivityRegistry.EnsureLoadedAsync();

        var activity = Activity;
        var activityName = Activity.GetName();
        var activityDisplayText = activity.GetDisplayText()?.Trim();
        var activityDescription = activity.GetDescription()?.Trim();
        var activityType = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var descriptor = ActivityRegistry.Find(activityType, activityVersion);
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        _label = !string.IsNullOrEmpty(activityDisplayText) ? activityDisplayText : !string.IsNullOrWhiteSpace(activityName) ? activityName : descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";
        _typeName = descriptor?.DisplayName ?? "Unknown Activity";
        _description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor?.Description ?? string.Empty;
        _showDescription = activity.GetShowDescription() == true;
        _color = displaySettings.Color;
        _icon = displaySettings.Icon;
        _activityDescriptor = descriptor!;
        _ports = descriptor != null ? ActivityPortService.GetPorts(new(descriptor, activity)).ToList() : [];

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
}