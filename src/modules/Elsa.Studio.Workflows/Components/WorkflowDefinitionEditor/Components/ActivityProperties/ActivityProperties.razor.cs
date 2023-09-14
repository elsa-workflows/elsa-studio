using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;

public partial class ActivityProperties
{
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Parameter] public JsonObject? Activity { get; set; }
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    [Parameter] public int VisiblePaneHeight { get; set; }
    
    private bool IsWorkflowAsActivity => ActivityDescriptor != null && ActivityDescriptor.CustomProperties.TryGetValue("RootType", out var value) && value.ConvertTo<string>() == "WorkflowDefinitionActivity";
}