using System.Diagnostics;
using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Settings;

/// <summary>
/// Displays the settings of a workflow.
/// </summary>
public partial class Settings
{
    private readonly JsonSerializerOptions _serializerOptions;

    /// <inheritdoc />
    public Settings()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        
    }
    
    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// An event raised when the workflow is updated.
    /// </summary>
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }

    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }

    [Inject] private IWorkflowActivationStrategyService WorkflowActivationStrategyService { get; set; } = default!;
    [Inject] private IIncidentStrategiesProvider IncidentStrategiesProvider { get; set; } = default!;
    [Inject] private ILogPersistenceStrategyService LogPersistenceStrategyService { get; set; } = default!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;
    private ICollection<WorkflowActivationStrategyDescriptor> _activationStrategies = new List<WorkflowActivationStrategyDescriptor>();
    private ICollection<IncidentStrategyDescriptor?> _incidentStrategies = new List<IncidentStrategyDescriptor?>();
    private ICollection<LogPersistenceStrategyDescriptor> _logPersistenceStrategyDescriptors = new List<LogPersistenceStrategyDescriptor>();
    private WorkflowActivationStrategyDescriptor? _selectedActivationStrategy;
    private IncidentStrategyDescriptor? _selectedIncidentStrategy;
    private LogPersistenceConfiguration? _logPersistenceConfiguration;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _activationStrategies = (await WorkflowActivationStrategyService.GetWorkflowActivationStrategiesAsync()).ToList();
        var incidentStrategies = (await IncidentStrategiesProvider.GetIncidentStrategiesAsync()).ToList();
        _incidentStrategies = new IncidentStrategyDescriptor?[] { default }.Concat(incidentStrategies).ToList();
        _logPersistenceStrategyDescriptors = (await LogPersistenceStrategyService.GetLogPersistenceStrategiesAsync()).ToList();

        _selectedActivationStrategy = _activationStrategies.FirstOrDefault(x => x.TypeName == WorkflowDefinition!.Options.ActivationStrategyType) ?? _activationStrategies.FirstOrDefault();
        _selectedIncidentStrategy = _incidentStrategies.FirstOrDefault(x => x?.TypeName == WorkflowDefinition!.Options.IncidentStrategyType) ?? _incidentStrategies.FirstOrDefault();
    }

    protected override Task OnParametersSetAsync()
    {
        _logPersistenceConfiguration = GetLogPersistenceConfiguration();
        return base.OnParametersSetAsync();
    }

    private LogPersistenceMode? GetLegacyGetLogPersistenceMode()
    {
        if (WorkflowDefinition!.CustomProperties.TryGetValue("logPersistenceMode", out var value) && value != null!)
        {
            var persistenceString = ((JsonElement)value).GetProperty("default");
            return (LogPersistenceMode)Enum.Parse(typeof(LogPersistenceMode), persistenceString.ToString());
        }

        return null;
    }

    private LogPersistenceConfiguration? GetLogPersistenceConfiguration()
    {
        if (WorkflowDefinition!.CustomProperties.TryGetValue("logPersistenceConfig", out var value) && value != null!)
        {
            var configJson = ((JsonElement)value).GetProperty("default");
            var config = JsonSerializer.Deserialize<LogPersistenceConfiguration>(configJson, _serializerOptions);
            return config;
        }

        var legacyMode = GetLegacyGetLogPersistenceMode();
        var strategyDescriptor = _logPersistenceStrategyDescriptors.FirstOrDefault(x => x.DisplayName == legacyMode.ToString()) ?? _logPersistenceStrategyDescriptors.FirstOrDefault();
        return new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = strategyDescriptor?.TypeName
        };
    }

    private async Task RaiseWorkflowUpdatedAsync()
    {
        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }

    private async Task UpdateLogPersistenceConfigAsync(LogPersistenceConfiguration newConfig)
    {
        _logPersistenceConfiguration = newConfig;
        WorkflowDefinition!.CustomProperties["logPersistenceConfig"] = new Dictionary<string, object?> { { "default", newConfig } };
        await RaiseWorkflowUpdatedAsync();
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

    private async Task OnLogPersistenceStrategyChanged(string? value)
    {
        var currentConfig = _logPersistenceConfiguration;
        var newConfig = new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = value,
            Expression = currentConfig?.Expression
        };
        await UpdateLogPersistenceConfigAsync(newConfig);
    }

    private async Task OnLogPersistenceExpressionChanged(Expression? value)
    {
        var currentConfig = _logPersistenceConfiguration;
        var newConfig = new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Expression,
            StrategyType = currentConfig?.StrategyType,
            Expression = value
        };
        await UpdateLogPersistenceConfigAsync(newConfig);
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