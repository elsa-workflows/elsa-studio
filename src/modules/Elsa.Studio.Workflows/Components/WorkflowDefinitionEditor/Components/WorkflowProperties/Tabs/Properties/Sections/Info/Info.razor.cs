using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.
    Sections.Info;

public partial class Info
{
    private IDictionary<string, DataPanelItem> _workflowInfo = new Dictionary<string, DataPanelItem>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    protected override void OnParametersSet()
    {
        _workflowInfo = new Dictionary<string, DataPanelItem>
        {
            ["Definition ID"] = new(WorkflowDefinition.DefinitionId),
            ["Version ID"] = new(WorkflowDefinition.Id),
            ["Version"] = new(WorkflowDefinition.Version.ToString()),
            ["Status"] = new(WorkflowDefinition.IsPublished ? "Published" : "Draft"),
            ["Readonly"] = new(WorkflowDefinition.IsReadonly ? "Yes" : "No")
        };
    }
}