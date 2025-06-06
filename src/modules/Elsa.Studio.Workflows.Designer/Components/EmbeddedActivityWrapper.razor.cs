using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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
    private string _label = null!;
    private string _description = null!;
    private bool _showDescription;
    private string _color = null!;
    private string? _icon;
    private ActivityDescriptor _activityDescriptor = null!;

    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = null!;
    [Parameter] public JsonObject Activity { get; set; } = null!;
    [Parameter] public bool IsSelected { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = null!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = null!;
    
    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;

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

        _label = activityDisplayText;
        _description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor.Description ?? string.Empty;
        _showDescription = activity.GetShowDescription() == true;
        _color = displaySettings.Color;
        _icon = displaySettings.Icon;
        _activityDescriptor = descriptor;
    }
}