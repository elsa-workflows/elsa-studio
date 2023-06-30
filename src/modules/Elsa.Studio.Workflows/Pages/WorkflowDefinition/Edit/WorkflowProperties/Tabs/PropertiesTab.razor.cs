using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.WorkflowProperties.Tabs;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class PropertiesTab
{
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
}