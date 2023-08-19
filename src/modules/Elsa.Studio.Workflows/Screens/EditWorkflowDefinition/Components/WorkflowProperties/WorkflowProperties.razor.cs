using System;
using System.Threading.Tasks;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components.WorkflowProperties;

public partial class WorkflowProperties
{
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
}