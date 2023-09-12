using System.Reflection;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.InputOutput.Components.Outcomes;

public partial class OutcomesSection
{
    private MudChipField<string> _chipField = default!;
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    
    private List<string> Outcomes => WorkflowDefinition.Outcomes.ToList();
    
    private async Task OnValuesChanges(List<string> values)
    {
        WorkflowDefinition.Outcomes = values;
        if (OnWorkflowDefinitionUpdated != null)
            await OnWorkflowDefinitionUpdated();
    }

    private void OnKeyDown(KeyboardEventArgs arg)
    {
        // TODO: This is a hack to get the chips to update when the user presses enter.
        // Ideally, we can configure this on MudChipField, but this is not currently supported. 
        if (arg.Key is not ("Enter" or "Tab")) return;
        
        var setChipsMethod = _chipField.GetType().GetMethod("SetChips", BindingFlags.Instance | BindingFlags.NonPublic);
        setChipsMethod!.Invoke(_chipField, null);
    }
}