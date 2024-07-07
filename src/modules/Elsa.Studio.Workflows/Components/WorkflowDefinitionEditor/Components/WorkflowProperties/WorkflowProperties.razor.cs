using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties;

public partial class WorkflowProperties
{
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    
    /// Gets or sets the callback that is invoked when the workflow definition is about to be reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionReverting { get; set; }

    /// Gets or sets the callback that is invoked when the workflow definition is reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionReverted { get; set; }
    
    /// Gets or sets a callback that is invoked when the workflow definition version is about to be deleted.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionDeleting { get; set; }
    
    /// Gets or sets a callback that is invoked when the workflow definition version is about to be deleted.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionDeleted { get; set; }
}