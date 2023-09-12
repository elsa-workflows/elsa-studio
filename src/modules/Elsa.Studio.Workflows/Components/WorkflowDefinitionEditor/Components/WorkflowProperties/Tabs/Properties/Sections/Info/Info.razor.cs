using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Info;

public partial class Info
{
    private IDictionary<string, string> _workflowInfo = new Dictionary<string, string>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    protected override void OnParametersSet()
    {
        _workflowInfo = new Dictionary<string, string>
        {
            ["Definition ID"] = WorkflowDefinition.DefinitionId,
            ["Version ID"] = WorkflowDefinition.Id, 
            ["Version"] = WorkflowDefinition.Version.ToString(),
            ["Status"] = WorkflowDefinition.IsPublished ? "Published" : "Draft", 
            ["Readonly"] = WorkflowDefinition.IsReadonly ? "Yes" : "No"
        };
    }
}