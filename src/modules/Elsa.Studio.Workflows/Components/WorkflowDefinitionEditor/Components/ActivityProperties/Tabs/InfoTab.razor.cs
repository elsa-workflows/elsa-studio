using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
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

    private IDictionary<string, DataPanelItem> ActivityInfo { get; } = new Dictionary<string, DataPanelItem>();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ActivityDescriptor.ConstructionProperties.TryGetValue("WorkflowDefinitionId", out var link);

        ActivityInfo["Type"] = new DataPanelItem(ActivityDescriptor.TypeName,
            link == null ? null : $"/workflows/definitions/{link}/edit");
        ActivityInfo["Description"] = new DataPanelItem(ActivityDescriptor.Description);
    }
}