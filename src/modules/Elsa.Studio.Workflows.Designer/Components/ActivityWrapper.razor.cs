using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
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
    private ICollection<Port> _ports = new List<Port>();

    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = default!;
    [Parameter] public JsonObject Activity { get; set; } = default!;
    [Parameter] public string? SelectedPortName { get; set; }
    [Parameter] public ActivityStats? Stats { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = default!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] IActivityPortService ActivityPortService { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    private bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;

    protected override async Task OnInitializedAsync()
    {
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
        var portProvider = ActivityPortService.GetProvider(_activityDescriptor.TypeName);
        var providerContext = new PortProviderContext(_activityDescriptor, Activity);
        portProvider.ClearPort(portName, providerContext);
        await UpdateSizeAsync();
    }
}