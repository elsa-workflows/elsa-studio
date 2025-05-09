using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ResilienceStrategies.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Serialization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// The Task tab for an activity.
/// </summary>
public partial class ResilienceTab
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    
    /// The activity.
    [Parameter] public JsonObject? Activity { get; set; }

    /// The activity descriptor.
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; } = null!;

    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    [Inject] private IResilienceStrategyCatalog ResilienceStrategyCatalog { get; set; } = null!;
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    private bool IsReadOnly => Workspace?.IsReadOnly == true;
    private ICollection<JsonObject> ResilienceStrategies { get; set; } = [];
    private ResilienceStrategyConfig? ResilienceStrategyConfig { get; set; }
    private Expression? Expression { get; set; }
    private string? ResilienceStrategyId { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;
        
        SetProperties();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var strategies = await ResilienceStrategyCatalog.ListAsync("HTTP");
        ResilienceStrategies = strategies.ToList();
    }
    
    private IDictionary<string, object> GetExpressionEditorProps()
    {
        var props = new Dictionary<string, object>();
        
        if(ActivityDescriptor != null) props[nameof(ActivityDescriptor)] = ActivityDescriptor;
        props["WorkflowDefinitionId"] = WorkflowDefinition!.DefinitionId;
        
        return props;
    }

    private async Task RaiseActivityUpdatedAsync()
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity!);
    }

    private async Task PersistStrategyConfigAsync()
    {
        var config = ResilienceStrategyConfig ?? new ResilienceStrategyConfig();
        config.Expression = Expression;
        config.StrategyId = ResilienceStrategyId;
        ResilienceStrategyConfig = config;
        Activity?.SetResilienceStrategy(config);

        await RaiseActivityUpdatedAsync();
    }

    private void SetProperties()
    {
        if (Activity == null)
            return;

        var config = Activity.GetResilienceStrategy();
        ResilienceStrategyConfig = config;
        Expression = config?.Expression;
        ResilienceStrategyId = config?.StrategyId;
    }

    private async Task OnExpressionChangedAsync(Expression? expression)
    {
        Expression = expression;
        await PersistStrategyConfigAsync();
    }

    private async Task OnStrategyChanged(string id)
    {
        ResilienceStrategyId = id;
        await PersistStrategyConfigAsync();
    }
}