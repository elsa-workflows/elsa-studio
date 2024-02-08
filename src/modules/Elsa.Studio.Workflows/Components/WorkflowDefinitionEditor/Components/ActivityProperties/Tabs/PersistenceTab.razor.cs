using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

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

    private PersistenceActivityConfiguration persistenceConfiguration = new PersistenceActivityConfiguration();

    private JsonSerializerOptions _serializerOptions = default;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        persistenceConfiguration = new PersistenceActivityConfiguration();
        _serializerOptions = new System.Text.Json.JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();

        SetPersistenceProperties();
       
    }

    private void SetPersistenceProperties()
    {
        var customProperties = Activity.GetProperty("customProperties");
        if (customProperties == null)
        {
            customProperties = new JsonObject(new List<KeyValuePair<string, JsonNode?>>());
            Activity.SetProperty(customProperties, "customProperties");
        }

        var persistence = (customProperties as JsonObject).GetProperty<PersistenceActivityConfiguration>(_serializerOptions, "persistence");
        if(persistence == null)
            persistence = new PersistenceActivityConfiguration();
        
        persistenceConfiguration = persistence;
        var props = persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity.SetProperty(props, "customProperties", "persistence");
    }

    private async Task OnBindingChanged()
    {
        var props = persistenceConfiguration.SerializeToNode(_serializerOptions);
        Activity.SetProperty(props, "customProperties", "persistence");

        await RaiseActivityUpdated();
    }

    private PersistenceStrategy InitorGetProperty(string propertyName, IDictionary<string,PersistenceStrategy> properties)
    {
        var prop = propertyName.Camelize();
        if (!properties.Any(o => o.Key == prop))
            properties[prop] = PersistenceStrategy.Default;
        return properties[prop];
    }
    private void SetProperty(string propertyName, IDictionary<string, PersistenceStrategy> properties, PersistenceStrategy value)
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
/// Define the Persistence Strategy Configuration for an activity
/// </summary>
public class PersistenceActivityConfiguration
{
    /// <summary>
    /// Default Configuration Strategy for the activity
    /// </summary>
    public PersistenceStrategy Default { get; set; } = PersistenceStrategy.Default;

    /// <summary>
    /// Define Configuration Strategy for each Input properties
    /// </summary>
    public Dictionary<string, PersistenceStrategy> Inputs { get; set; } = new Dictionary<string, PersistenceStrategy>();
    
    /// <summary>
    /// Define Configuration Strategy for each Output properties
    /// </summary>
    public Dictionary<string, PersistenceStrategy> Outputs { get; set; } = new Dictionary<string, PersistenceStrategy>();
}

