using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;

/// <summary>
/// Displays the outputs of an activity.
/// </summary>
public partial class OutputsTab
{
    private ICollection<BindingTargetGroup> _bindingTargetGroups = new List<BindingTargetGroup>();
    private ICollection<BindingTargetOption> _bindingTargetOptions = new List<BindingTargetOption>();
    private IDictionary<string, VariableTypeDescriptor> _variableTypes = new Dictionary<string, VariableTypeDescriptor>();

    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = default!;
    
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    
    /// <summary>
    /// An event raised when the activity is updated.
    /// </summary>
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    
    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }

    [Inject] IVariableTypeService VariableTypeService { get; set; } = default!;
    [Inject] private ILocalizer _localizer { get; set; } = default!;

    private IReadOnlyCollection<OutputDescriptor> OutputDescriptors => ActivityDescriptor.Outputs;
    private bool IsReadOnly => Workspace?.IsReadOnly == true;

    /// <inheritdoc />
    protected override bool ShouldRender() => WorkflowDefinition != null! && Activity != null! && ActivityDescriptor != null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToDictionary(x => x.TypeName);
    }

    /// <inheritdoc />
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

        if (variableBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup(_localizer["Variables"], BindingKind.Variable, variableBindingTargets));
        
        if (outputBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup(_localizer["Outputs"], BindingKind.Output, outputBindingTargets));

        _bindingTargetGroups = bindingTargetGroups;
        _bindingTargetOptions = variableBindingTargets.Concat(outputBindingTargets).ToList();
        return Task.CompletedTask;
    }

    private async Task OnBindingChanged(BindingTargetOption? bindingTargetOption, OutputDescriptor outputDescriptor)
    {
        var activity = Activity;
        var propertyName = outputDescriptor.Name.Camelize();

        var activityOutput = bindingTargetOption == null
            ? default
            : new ActivityOutput
            {
                TypeName = outputDescriptor.TypeName,
                MemoryReference = new MemoryReference
                {
                    Id = bindingTargetOption.Value
                }
            };

        activity.SetProperty(activityOutput!.SerializeToNode(), propertyName);

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}