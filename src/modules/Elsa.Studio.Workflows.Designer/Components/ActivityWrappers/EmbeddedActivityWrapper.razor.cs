using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components.ActivityWrappers;

public abstract class EmbeddedActivityWrapperBase : StudioComponentBase
{
    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = null!;
    [Parameter] public JsonObject Activity { get; set; } = null!;
    [Parameter] public bool IsSelected { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = null!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = null!;

    protected bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;
    protected string Label { get; private set; } = null!;
    protected string Description { get; private set; } = null!;
    protected bool ShowDescription { get; private set; }
    protected string Color  { get; private set; } = null!;
    protected string? Icon  { get; private set; }
    protected ActivityDescriptor ActivityDescriptor  { get; private set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        
        var activity = Activity;
        var activityType = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var descriptor = (ActivityRegistry.Find(activityType, activityVersion) ?? ActivityRegistry.Find("Elsa.UnknownActivity"))!;
        var activityDisplayText = activity.GetDisplayText()?.Trim() ?? activity.GetName() ?? descriptor.DisplayName ?? descriptor.Name;
        var activityDescription = activity.GetDescription()?.Trim();
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        Label = activityDisplayText;
        Description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor.Description ?? string.Empty;
        ShowDescription = activity.GetShowDescription() == true;
        Color = displaySettings.Color;
        Icon = displaySettings.Icon;
        ActivityDescriptor = descriptor;
    }
}