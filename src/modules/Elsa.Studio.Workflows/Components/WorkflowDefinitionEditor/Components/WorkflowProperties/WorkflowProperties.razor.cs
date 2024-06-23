using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties;

public partial class WorkflowProperties
{
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    
    /// Gets or sets the callback that is invoked when the workflow definition is about to be reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionReversionEventArgs> WorkflowDefinitionReverting { get; set; }

    /// Gets or sets the callback that is invoked when the workflow definition is reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionReversionEventArgs> WorkflowDefinitionReverted { get; set; }
}