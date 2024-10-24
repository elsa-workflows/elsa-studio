using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// Represents the persistence tab.
/// </summary>
public partial class LogPersistenceTab
{
    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter] public JsonObject? Activity { get; set; }

    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }

    [Inject] private ILogPersistenceStrategyService LogPersistenceStrategyService { get; set; } = default!;

    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private bool IsReadOnly => Workspace?.IsReadOnly == true;
    private LegacyPersistenceActivityConfiguration _legacyPersistenceConfiguration = new();
    private PersistenceActivityConfiguration _persistenceConfiguration = new();
    private JsonSerializerOptions _serializerOptions = default!;
    private ICollection<LogPersistenceStrategyDescriptor> _logPersistenceStrategyDescriptors = new List<LogPersistenceStrategyDescriptor>();
    private const string LogPersistenceModeKey = "logPersistenceMode";
    private const string LogPersistenceStrategyKey = "logPersistenceStrategy";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _legacyPersistenceConfiguration = new LegacyPersistenceActivityConfiguration();
        _persistenceConfiguration = new PersistenceActivityConfiguration();
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        _logPersistenceStrategyDescriptors = (await LogPersistenceStrategyService.GetLogPersistenceStrategiesAsync()).ToList();
        await base.OnInitializedAsync();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();

        SetPersistenceProperties();
    }

    private async Task OnDefaultStrategyChanged(string? value)
    {
        _persistenceConfiguration.DefaultStrategy = value;
        await PersistLogPersistenceStrategies();
    }

    private async Task OnPropertyStrategyChanged(string propertyName, IDictionary<string, string?> properties, string? value)
    {
        SetLogPersistenceStrategyTypeName(propertyName, properties, value);
        await PersistLogPersistenceStrategies();
    }

    private async Task PersistLogPersistenceStrategies()
    {
        var props = _persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity?.SetProperty(props, "customProperties", LogPersistenceStrategyKey);

        await RaiseActivityUpdated();
    }

    private void SetPersistenceProperties()
    {
        var customProperties = GetCustomProperties();
        var legacyPersistence = ((JsonObject)customProperties).GetProperty<LegacyPersistenceActivityConfiguration>(_serializerOptions, LogPersistenceModeKey) ?? new LegacyPersistenceActivityConfiguration();
        var persistence = ((JsonObject)customProperties).GetProperty<PersistenceActivityConfiguration>(_serializerOptions, LogPersistenceStrategyKey) ?? new PersistenceActivityConfiguration();

        _legacyPersistenceConfiguration = legacyPersistence;
        _persistenceConfiguration = persistence;
        UpgradeLegacyModel(_persistenceConfiguration, _legacyPersistenceConfiguration);
        
        if (Activity == null)
            return;

        var serializedLegacyPersistenceConfiguration = _legacyPersistenceConfiguration.SerializeToNode(_serializerOptions);
        var serializedPersistenceConfiguration = _persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity.SetProperty(serializedLegacyPersistenceConfiguration, "customProperties", LogPersistenceModeKey);
        Activity.SetProperty(serializedPersistenceConfiguration, "customProperties", LogPersistenceStrategyKey);
    }

    private void UpgradeLegacyModel(PersistenceActivityConfiguration newModel, LegacyPersistenceActivityConfiguration oldModel)
    {
        // Update the new model with the legacy model.
        foreach (var input in oldModel.Inputs)
        {
            if (newModel.Inputs.ContainsKey(input.Key))
                continue;
            newModel.Inputs[input.Key] = input.Value.ToString();
        }

        foreach (var output in oldModel.Outputs)
        {
            if (newModel.Outputs.ContainsKey(output.Key))
                continue;
            newModel.Outputs[output.Key] = output.Value.ToString();
        }
    }

    private JsonNode GetCustomProperties()
    {
        var customProperties = Activity?.GetProperty("customProperties");
        if (customProperties != null) return customProperties;
        customProperties = new JsonObject(new List<KeyValuePair<string, JsonNode?>>());
        Activity?.SetProperty(customProperties, "customProperties");
        return customProperties;
    }

    private string? GetPropertyLogPersistenceStrategyTypeName(string propertyName, IDictionary<string, string?> properties)
    {
        var prop = propertyName.Camelize();
        return properties.TryGetValue(prop, out var v) ? v : null;
    }

    private void SetLogPersistenceStrategyTypeName(string propertyName, IDictionary<string, string?> properties, string? value)
    {
        var prop = propertyName.Camelize();
        properties[prop] = value;
    }

    [Obsolete]
    private LogPersistenceMode GetPropertyLogPersistenceMode(string propertyName, IDictionary<string, LogPersistenceMode> properties)
    {
        var prop = propertyName.Camelize();
        if (properties.All(o => o.Key != prop))
            properties[prop] = LogPersistenceMode.Inherit;
        return properties[prop];
    }

    [Obsolete]
    private void SetLogPersistenceMode(string propertyName, IDictionary<string, LogPersistenceMode> properties, LogPersistenceMode value)
    {
        var prop = propertyName.Camelize();
        properties[prop] = value;
    }

    private async Task RaiseActivityUpdated()
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity!);
    }
}

/// <summary>
/// Defines the Persistence Strategy Configuration for an activity
/// </summary>
[Obsolete]
public class LegacyPersistenceActivityConfiguration
{
    /// <summary>
    /// Default Configuration Strategy for the activity
    /// </summary>
    public LogPersistenceMode Default { get; set; } = LogPersistenceMode.Inherit;

    /// <summary>
    /// Define Configuration Strategy for each Input properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Inputs { get; set; } = new Dictionary<string, LogPersistenceMode>();

    /// <summary>
    /// Define Configuration Strategy for each Output properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Outputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
}

/// <summary>
/// Defines the Persistence Strategy Configuration for an activity
/// </summary>
public class PersistenceActivityConfiguration
{
    /// <summary>
    /// Default strategy type name for the entire activity.
    /// </summary>
    public string? DefaultStrategy { get; set; }

    /// <summary>
    /// Strategy type names for individual Input properties.
    /// </summary>
    public IDictionary<string, string?> Inputs { get; set; } = new Dictionary<string, string?>();

    /// <summary>
    /// Strategy type names for individual Output properties.
    /// </summary>
    public IDictionary<string, string?> Outputs { get; set; } = new Dictionary<string, string?>();
}