using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Settings;

/// <summary>
/// Displays the settings of a workflow.
/// </summary>
public partial class Settings
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    
    /// <summary>
    /// An event raised when the workflow is updated.
    /// </summary>
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    
    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }
    
    [Inject] private IWorkflowActivationStrategyService WorkflowActivationStrategyService { get; set; } = default!;
    [Inject] private IIncidentStrategiesProvider IncidentStrategiesProvider { get; set; } = default!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;
    private ICollection<WorkflowActivationStrategyDescriptor> _activationStrategies = new List<WorkflowActivationStrategyDescriptor>();
    private ICollection<IncidentStrategyDescriptor?> _incidentStrategies = new List<IncidentStrategyDescriptor?>();
    private WorkflowActivationStrategyDescriptor? _selectedActivationStrategy;
    private IncidentStrategyDescriptor? _selectedIncidentStrategy;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        // Load activation strategies.
        _activationStrategies = (await WorkflowActivationStrategyService.GetWorkflowActivationStrategiesAsync()).ToList();
        
        // Load incident strategies.
        var incidentStrategies = (await IncidentStrategiesProvider.GetIncidentStrategiesAsync()).ToList();
        _incidentStrategies = new IncidentStrategyDescriptor?[] { default }.Concat(incidentStrategies).ToList();
        
        // Select the current activation strategy.
        _selectedActivationStrategy = _activationStrategies.FirstOrDefault(x => x.TypeName == WorkflowDefinition!.Options.ActivationStrategyType) ?? _activationStrategies.FirstOrDefault();
        
        // Select the current incident strategy.
        _selectedIncidentStrategy = _incidentStrategies.FirstOrDefault(x => x?.TypeName == WorkflowDefinition!.Options.IncidentStrategyType) ?? _incidentStrategies.FirstOrDefault();
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
    
    private async Task OnIncidentStrategyChanged(IncidentStrategyDescriptor? value)
    {
        _selectedIncidentStrategy = value;
        WorkflowDefinition!.Options.IncidentStrategyType = value?.TypeName;
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

    private async Task OnCategoryChanged(string value)
    {
        WorkflowDefinition!.Options.ActivityCategory = value;
        await RaiseWorkflowUpdatedAsync();
    }
}