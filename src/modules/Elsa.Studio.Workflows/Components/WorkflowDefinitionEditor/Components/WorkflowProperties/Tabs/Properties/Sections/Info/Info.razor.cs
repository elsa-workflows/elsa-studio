using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.
    Sections.Info;

public partial class Info
{
    private DataPanelModel _workflowInfo = new ();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _workflowInfo =
        [
            new DataPanelItem("Definition ID", WorkflowDefinition.DefinitionId),
            new DataPanelItem("Version ID", WorkflowDefinition.Id),
            new DataPanelItem("Version", WorkflowDefinition.Version.ToString()),
            new DataPanelItem("Status", WorkflowDefinition.IsPublished ? "Published" : "Draft"),
            new DataPanelItem("Readonly", WorkflowDefinition.IsReadonly ? "Yes" : "No")
        ];
    }
}