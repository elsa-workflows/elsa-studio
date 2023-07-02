using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.ActivityProperties.Tabs.Outputs.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.ActivityProperties.Tabs.Outputs.Components;

public partial class OutputsTab
{
    private ICollection<BindingTargetGroup> _bindingTargetGroups = new List<BindingTargetGroup>();
    private ICollection<BindingTargetOption> _bindingTargetOptions = new List<BindingTargetOption>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Activity Activity { get; set; } = default!;
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    [Parameter] public Func<Activity, Task>? OnActivityUpdated { get; set; }
    
    private IReadOnlyCollection<OutputDescriptor> OutputDescriptors => ActivityDescriptor.Outputs;

    protected override bool ShouldRender() => WorkflowDefinition != null! && Activity != null! && ActivityDescriptor != null!;

    protected override Task OnParametersSetAsync()
    {
        var bindingTargetGroups = new List<BindingTargetGroup>();
        var variables = WorkflowDefinition.Variables;
        var outputDefinitions = WorkflowDefinition.Outputs;

        var variableBindingTargets = variables
            .Select(variable => new BindingTargetOption(variable.Name, variable.Id))
            .ToList();

        var outputBindingTargets = outputDefinitions
            .Select(outputDefinition => new BindingTargetOption(outputDefinition.DisplayName, outputDefinition.Name))
            .ToList();
        
        if(variableBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup("Variables", BindingKind.Variable, variableBindingTargets));
        if(outputBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup("Outputs", BindingKind.Output, outputBindingTargets));
        
        _bindingTargetGroups = bindingTargetGroups;
        _bindingTargetOptions = variableBindingTargets.Concat(outputBindingTargets).ToList();
        return Task.CompletedTask;
    }

    private async Task OnBindingChanged(BindingTargetOption bindingTargetOption, OutputDescriptor outputDescriptor)
    {
        var activity = Activity;
        var propertyName = outputDescriptor.Name.Camelize();

        activity[propertyName] = new ActivityOutput
        {
            TypeName = outputDescriptor.TypeName,
            MemoryReference = new MemoryReference
            {
                Id = bindingTargetOption.Value
            }
        };
        
        if(OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}