using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// The info tab for an activity.
/// </summary>
public partial class InfoTab
{
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor ActivityDescriptor { get; set; } = default!;

    private DataPanelModel ActivityInfo { get; } = new();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ActivityDescriptor.ConstructionProperties.TryGetValue("WorkflowDefinitionId", out var link);

        ActivityInfo.Clear(); 
        
        ActivityInfo.Add(Localizer["Type"], ActivityDescriptor.TypeName, link == null ? null : $"/workflows/definitions/{link}/edit");
        ActivityInfo.Add(Localizer["Description"], ActivityDescriptor.Description);
    }
}