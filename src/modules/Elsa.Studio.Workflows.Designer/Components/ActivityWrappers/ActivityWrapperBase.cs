using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components.ActivityWrappers;

/// <summary>
/// A wrapper for an activity component.
/// </summary>
public abstract class ActivityWrapperBase : StudioComponentBase
{
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    

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

    [Inject] protected DesignerJsInterop DesignerInterop { get; set; } = null!;
    [Inject] protected IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] protected IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] protected IActivityPortService ActivityPortService { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Provides the can start workflow.
    /// </summary>
    protected bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    protected string Label { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    protected string TypeName { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    protected string Description { get; private set; } = null!;
    /// <summary>
    /// Indicates whether show description.
    /// </summary>
    protected bool ShowDescription { get; private set; }
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    protected string Color { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    protected string? Icon { get; private set; }
    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    protected ActivityDescriptor? ActivityDescriptor { get; private set; }
    /// <summary>
    /// Gets or sets the ports.
    /// </summary>
    protected ICollection<Port> Ports { get; private set; } = new List<Port>();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ActivityJson))
            Activity = JsonSerializer.Deserialize<JsonObject>(ActivityJson, _serializerOptions)!;

        await ActivityRegistry.EnsureLoadedAsync();

        var activity = Activity;
        var activityDisplayText = activity.GetDisplayText()?.Trim();
        var activityDescription = activity.GetDescription()?.Trim();
        var activityType = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var descriptor = ActivityRegistry.Find(activityType, activityVersion);
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        Label = !string.IsNullOrEmpty(activityDisplayText) ? activityDisplayText : descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";
        TypeName = descriptor?.DisplayName ?? "Unknown Activity";
        Description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor?.Description ?? string.Empty;
        ShowDescription = activity.GetShowDescription() == true;
        Color = displaySettings.Color;
        Icon = displaySettings.Icon;
        ActivityDescriptor = descriptor!;
        Ports = descriptor != null ? ActivityPortService.GetPorts(new PortProviderContext(descriptor, activity)).ToList() : [];

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