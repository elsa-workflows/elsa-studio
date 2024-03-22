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

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// Represents the persistence tab.
/// </summary>
public partial class PersistenceTab
{
    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }
    /// <summary>
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter]
    public JsonObject? Activity { get; set; }
    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }
    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor? ActivityDescriptor { get; set; }
    
    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private bool IsReadOnly => Workspace?.IsReadOnly == true;
    private PersistenceActivityConfiguration _persistenceConfiguration = new();
    private JsonSerializerOptions _serializerOptions = default!;
    private const string LogPersistenceModeKey = "logPersistenceMode";
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _persistenceConfiguration = new PersistenceActivityConfiguration();
        _serializerOptions = new System.Text.Json.JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());

        base.OnInitialized();
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

    private void SetPersistenceProperties()
    {
        var customProperties = Activity?.GetProperty("customProperties");
        if (customProperties == null)
        {
            customProperties = new JsonObject(new List<KeyValuePair<string, JsonNode?>>());
            Activity?.SetProperty(customProperties, "customProperties");
        }

        var persistence = ((JsonObject)customProperties).GetProperty<PersistenceActivityConfiguration>(_serializerOptions, LogPersistenceModeKey) ?? new PersistenceActivityConfiguration();

        _persistenceConfiguration = persistence;
        var props = _persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity?.SetProperty(props, "customProperties", LogPersistenceModeKey);
    }

    private async Task OnBindingChanged()
    {
        var props = _persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity?.SetProperty(props, "customProperties", LogPersistenceModeKey);

        await RaiseActivityUpdated();
    }

    private LogPersistenceMode GetProperty(string propertyName, IDictionary<string,LogPersistenceMode> properties)
    {
        var prop = propertyName.Camelize();
        if (properties.All(o => o.Key != prop))
            properties[prop] = LogPersistenceMode.Default;
        return properties[prop];
    }
    private void SetProperty(string propertyName, IDictionary<string, LogPersistenceMode> properties, LogPersistenceMode value)
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
public class PersistenceActivityConfiguration
{
    /// <summary>
    /// Default Configuration Strategy for the activity
    /// </summary>
    public LogPersistenceMode Default { get; set; } = LogPersistenceMode.Default;

    /// <summary>
    /// Define Configuration Strategy for each Input properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Inputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
    
    /// <summary>
    /// Define Configuration Strategy for each Output properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Outputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
}

