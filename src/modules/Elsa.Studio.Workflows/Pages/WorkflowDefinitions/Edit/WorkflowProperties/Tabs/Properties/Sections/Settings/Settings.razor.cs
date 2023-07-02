using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.Properties.Sections.Settings;

public partial class Settings
{
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    [Inject] private IWorkflowActivationStrategyService WorkflowActivationStrategyService { get; set; } = default!;

    private ICollection<WorkflowActivationStrategyDescriptor> _activationStrategies = new List<WorkflowActivationStrategyDescriptor>();
    private WorkflowActivationStrategyDescriptor? _selectedActivationStrategy;
    
    protected override async Task OnInitializedAsync()
    {
        _activationStrategies = (await WorkflowActivationStrategyService.GetWorkflowActivationStrategiesAsync()).ToList();
        _selectedActivationStrategy = _activationStrategies.FirstOrDefault(x => x.TypeName == WorkflowDefinition!.Options.ActivationStrategyType) ?? _activationStrategies.FirstOrDefault();
    }
    
    private async Task RaiseWorkflowUpdatedAsync()
    {
        if (OnWorkflowDefinitionUpdated != null)
            await OnWorkflowDefinitionUpdated();
    }

    private async Task OnActivationStrategyChanged(WorkflowActivationStrategyDescriptor value)
    {
        _selectedActivationStrategy = value;
        WorkflowDefinition!.Options.ActivationStrategyType = value.TypeName;
        await RaiseWorkflowUpdatedAsync();
    }

    private async Task OnUsableAsActivityCheckChanged(bool? value)
    {
        WorkflowDefinition!.Options.UsableAsActivity = value;
        await RaiseWorkflowUpdatedAsync();
    }
    
    private async Task OnAutoUpdateConsumingWorkflowsCheckChanged(bool? value)
    {
        WorkflowDefinition!.Options.AutoUpdateConsumingWorkflows = value == true;
        await RaiseWorkflowUpdatedAsync();
    }
}