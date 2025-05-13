using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Elsa.Api.Client.Resources.CommitStrategies.Models;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Serialization;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Settings;

/// <summary>
/// Displays the settings of a workflow.
/// </summary>
public partial class Settings
{
    private readonly JsonSerializerOptions _serializerOptions = SerializerOptions.LogPersistenceConfigSerializerOptions;

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

    [Inject] private IWorkflowActivationStrategyService WorkflowActivationStrategyService { get; set; } = null!;
    [Inject] private IIncidentStrategiesProvider IncidentStrategiesProvider { get; set; } = null!;
    [Inject] private ILogPersistenceStrategyService LogPersistenceStrategyService { get; set; } = null!;
    [Inject] private ICommitStrategiesProvider CommitStrategiesProvider { get; set; } = null!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;
    private ICollection<WorkflowActivationStrategyDescriptor> _activationStrategies = new List<WorkflowActivationStrategyDescriptor>();
    private ICollection<IncidentStrategyDescriptor?> _incidentStrategies = new List<IncidentStrategyDescriptor?>();
    private ICollection<LogPersistenceStrategyDescriptor> _logPersistenceStrategyDescriptors = new List<LogPersistenceStrategyDescriptor>();
    private ICollection<CommitStrategyDescriptor?> _commitStrategies = new List<CommitStrategyDescriptor?>();
    private WorkflowActivationStrategyDescriptor? _selectedActivationStrategy;
    private IncidentStrategyDescriptor? _selectedIncidentStrategy;
    private LogPersistenceStrategyDescriptor? _selectedLogPersistenceStrategy;
    private LogPersistenceConfiguration? _logPersistenceConfiguration;
    private CommitStrategyDescriptor? _selectedCommitStrategy;
    private ExpressionEditor _logPersistenceExpressionEditor = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _activationStrategies = (await WorkflowActivationStrategyService.GetWorkflowActivationStrategiesAsync()).ToList();
        var incidentStrategies = (await IncidentStrategiesProvider.GetIncidentStrategiesAsync()).ToList();
        var commitStrategies = (await CommitStrategiesProvider.GetWorkflowCommitStrategiesAsync()).ToList();
        _incidentStrategies = new IncidentStrategyDescriptor?[] { null }.Concat(incidentStrategies).ToList();
        _commitStrategies = new CommitStrategyDescriptor?[] { null }.Concat(commitStrategies).ToList();
        _logPersistenceStrategyDescriptors = (await LogPersistenceStrategyService.GetLogPersistenceStrategiesAsync()).ToList();
        _selectedActivationStrategy = _activationStrategies.FirstOrDefault(x => x.TypeName == WorkflowDefinition!.Options.ActivationStrategyType) ?? _activationStrategies.FirstOrDefault();
        _selectedIncidentStrategy = _incidentStrategies.FirstOrDefault(x => x?.TypeName == WorkflowDefinition!.Options.IncidentStrategyType) ?? _incidentStrategies.FirstOrDefault();
        _selectedCommitStrategy = _commitStrategies.FirstOrDefault(x => x?.Name == WorkflowDefinition!.Options.CommitStrategyName) ?? _commitStrategies.FirstOrDefault();
        _logPersistenceConfiguration = GetLogPersistenceConfiguration();
        _selectedLogPersistenceStrategy = _logPersistenceStrategyDescriptors.FirstOrDefault(x => x.TypeName == _logPersistenceConfiguration?.StrategyType) ?? _logPersistenceStrategyDescriptors.FirstOrDefault();
        await _logPersistenceExpressionEditor.UpdateAsync(_logPersistenceConfiguration?.Expression);
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
        return new()
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
        var dictionary = new Dictionary<string, object?> { { "default", newConfig } };
        var dictionaryJson = JsonSerializer.SerializeToElement(dictionary, _serializerOptions);
        WorkflowDefinition!.CustomProperties["logPersistenceConfig"] = dictionaryJson;
        await RaiseWorkflowUpdatedAsync();
    }

    private IDictionary<string, object> GetExpressionEditorProps()
    {
        var props = new Dictionary<string, object>
        {
            ["WorkflowDefinitionId"] = WorkflowDefinition!.DefinitionId
        };

        return props;
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

    private async Task OnLogPersistenceStrategySelectionChanged(LogPersistenceStrategyDescriptor? value)
    {
        _selectedLogPersistenceStrategy = value;
        var currentConfig = _logPersistenceConfiguration;
        var newConfig = new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = value?.TypeName,
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

    private async Task OnCommitStrategySelectionChanged(CommitStrategyDescriptor? value)
    {
        _selectedCommitStrategy = value;
        WorkflowDefinition!.Options.CommitStrategyName = value?.Name;
        await RaiseWorkflowUpdatedAsync();
    }
}