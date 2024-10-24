using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Settings;

/// <summary>
/// Displays the settings of a workflow.
/// </summary>
public partial class Settings
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// An event raised when the workflow is updated.
    /// </summary>
    [Parameter]
    public EventCallback WorkflowDefinitionUpdated { get; set; }

    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter]
    public IWorkspace? Workspace { get; set; }

    [Inject] private IWorkflowActivationStrategyService WorkflowActivationStrategyService { get; set; } = default!;
    [Inject] private IIncidentStrategiesProvider IncidentStrategiesProvider { get; set; } = default!;
    [Inject] private ILogPersistenceStrategyService LogPersistenceStrategyService { get; set; } = default!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;
    private ICollection<WorkflowActivationStrategyDescriptor> _activationStrategies = new List<WorkflowActivationStrategyDescriptor>();
    private ICollection<IncidentStrategyDescriptor?> _incidentStrategies = new List<IncidentStrategyDescriptor?>();
    private ICollection<LogPersistenceStrategyDescriptor> _logPersistenceStrategyDescriptors = new List<LogPersistenceStrategyDescriptor>();
    private WorkflowActivationStrategyDescriptor? _selectedActivationStrategy;
    private IncidentStrategyDescriptor? _selectedIncidentStrategy;
    private LogPersistenceStrategyDescriptor? _selectedLogPersistenceStrategy;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _activationStrategies = (await WorkflowActivationStrategyService.GetWorkflowActivationStrategiesAsync()).ToList();
        var incidentStrategies = (await IncidentStrategiesProvider.GetIncidentStrategiesAsync()).ToList();
        _incidentStrategies = new IncidentStrategyDescriptor?[] { default }.Concat(incidentStrategies).ToList();
        _logPersistenceStrategyDescriptors = (await LogPersistenceStrategyService.GetLogPersistenceStrategiesAsync()).ToList();
        
        _selectedActivationStrategy = _activationStrategies.FirstOrDefault(x => x.TypeName == WorkflowDefinition!.Options.ActivationStrategyType) ?? _activationStrategies.FirstOrDefault();
        _selectedIncidentStrategy = _incidentStrategies.FirstOrDefault(x => x?.TypeName == WorkflowDefinition!.Options.IncidentStrategyType) ?? _incidentStrategies.FirstOrDefault();
        _selectedLogPersistenceStrategy = GetLogPersistenceStrategy();
    }

    private LogPersistenceMode? LegacyGetLogPersistenceMode()
    {
        if (WorkflowDefinition!.CustomProperties.TryGetValue("logPersistenceMode", out var value) && value != null!)
        {
            var persistenceString = ((JsonElement)value).GetProperty("default");
            return (LogPersistenceMode)Enum.Parse(typeof(LogPersistenceMode), persistenceString.ToString());
        }
        
        return null;
    }
    
    private LogPersistenceStrategyDescriptor? GetLogPersistenceStrategy()
    {
        if (WorkflowDefinition!.CustomProperties.TryGetValue("logPersistenceStrategy", out var value) && value != null!)
        {
            var persistenceString = ((JsonElement)value).GetProperty("default");
            return _logPersistenceStrategyDescriptors.FirstOrDefault(x => x.TypeName == persistenceString.ToString());
        }
     
        var legacyMode = LegacyGetLogPersistenceMode();
        return _logPersistenceStrategyDescriptors.FirstOrDefault(x => x.DisplayName == legacyMode.ToString()) ?? _logPersistenceStrategyDescriptors.FirstOrDefault();
    }

    private async Task RaiseWorkflowUpdatedAsync()
    {
        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
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
    
    private async Task OnLogPersistenceStrategyChanged(LogPersistenceStrategyDescriptor? value)
    {
        _selectedLogPersistenceStrategy = value;
        WorkflowDefinition!.CustomProperties["logPersistenceStrategy"] = new Dictionary<string, object?> { { "default", value?.TypeName } };
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