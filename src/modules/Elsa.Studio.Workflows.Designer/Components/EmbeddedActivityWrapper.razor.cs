using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.Designer.Components;

public partial class EmbeddedActivityWrapper
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
    [Parameter] public bool IsSelected { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = default!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
    
    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;
    
    protected override async Task OnInitializedAsync()
    {
        var activity = Activity;
        var activityType = activity.GetTypeName();
        var descriptor = (await ActivityRegistry.FindAsync(activityType))!;
        var activityDisplayText = activity.GetDisplayText()?.Trim() ?? activity.GetName() ?? descriptor.DisplayName;
        var activityDescription = activity.GetDescription()?.Trim();
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        _label = !string.IsNullOrEmpty(activityDisplayText) ? activityDisplayText : descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";
        _description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor?.Description ?? string.Empty;
        _showDescription = activity.GetShowDescription() == true;
        _color = displaySettings.Color;
        _icon = displaySettings.Icon;
        _activityDescriptor = descriptor;
    }
}