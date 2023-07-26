using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.Properties;

public partial class PropertiesTab
{
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
}