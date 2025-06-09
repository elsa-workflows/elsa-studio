using System.Data;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.TestRun;

/// <summary>
/// The info tab for an activity.
/// </summary>
public partial class TestRunTab
{
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = null!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;
    
    private DataPanelModel TestRunResults { get; } = new();
    private bool IsRunning { get; set; }

    private async Task OnTestRunClick()
    {
        IsRunning = true;
        StateHasChanged();
        await Task.Delay(1000);
        IsRunning = false;
        StateHasChanged();
    }
}