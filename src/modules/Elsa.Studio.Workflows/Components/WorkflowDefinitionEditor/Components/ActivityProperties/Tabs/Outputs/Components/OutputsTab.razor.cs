using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.OutputConverters.Models;
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
    private readonly IDictionary<string, OutputConverterState> _converterStates = new Dictionary<string, OutputConverterState>();

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
    [Inject] IOutputConverterService OutputConverterService { get; set; } = default!;

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
    protected override async Task OnParametersSetAsync()
    {
        var bindingTargetGroups = new List<BindingTargetGroup>();
        var variables = WorkflowDefinition.Variables;
        var outputDefinitions = WorkflowDefinition.Outputs;

        var variableBindingTargets = variables
            .Select(variable => new BindingTargetOption(variable.Name, variable.Id, variable.TypeName, variable.IsArray))
            .ToList();

        var outputBindingTargets = outputDefinitions
            .Select(outputDefinition => new BindingTargetOption(outputDefinition.DisplayName, outputDefinition.Name, outputDefinition.Type, outputDefinition.IsArray))
            .ToList();

        if (variableBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup("Variables", BindingKind.Variable, variableBindingTargets));
        
        if (outputBindingTargets.Any()) bindingTargetGroups.Add(new BindingTargetGroup("Outputs", BindingKind.Output, outputBindingTargets));

        _bindingTargetGroups = bindingTargetGroups;
        _bindingTargetOptions = variableBindingTargets.Concat(outputBindingTargets).ToList();

        foreach (var outputDescriptor in OutputDescriptors)
        {
            var propertyName = outputDescriptor.Name.Camelize();
            var binding = Activity.GetProperty<ActivityOutput>(propertyName);
            var target = _bindingTargetOptions.FirstOrDefault(x => x.Value == binding?.MemoryReference?.Id);
            await LoadConvertersAsync(outputDescriptor, target, clearIncompatibleConverter: false);
        }
    }

    private async Task OnBindingChanged(BindingTargetOption? bindingTargetOption, OutputDescriptor outputDescriptor)
    {
        var activity = Activity;
        var propertyName = outputDescriptor.Name.Camelize();

        if (bindingTargetOption == null)
        {
            activity.SetProperty(default(JsonNode), propertyName);
            await RaiseActivityUpdatedAsync(activity);
            return;
        }

        var currentBinding = activity.GetProperty<JsonObject>(propertyName);
        var activityOutput = new JsonObject
        {
            ["typeName"] = outputDescriptor.TypeName,
            ["memoryReference"] = new JsonObject { ["id"] = bindingTargetOption.Value }
        };

        if (currentBinding?["converter"] is JsonObject converter)
            activityOutput["converter"] = converter.DeepClone();

        activity.SetProperty(activityOutput, propertyName);
        await RaiseActivityUpdatedAsync(activity);
        await LoadConvertersAsync(outputDescriptor, bindingTargetOption, clearIncompatibleConverter: true);
    }

    private async Task OnConverterChanged(string? converterId, OutputDescriptor outputDescriptor)
    {
        var propertyName = outputDescriptor.Name.Camelize();
        var binding = Activity.GetProperty<JsonObject>(propertyName);
        if (binding == null)
            return;

        if (string.IsNullOrWhiteSpace(converterId))
        {
            binding.Remove("converter");
        }
        else
        {
            var existingConverter = binding["converter"] as JsonObject;
            var settings = existingConverter?["id"]?.GetValue<string>() == converterId
                ? existingConverter["settings"]?.DeepClone()
                : new JsonObject();
            binding["converter"] = new JsonObject
            {
                ["id"] = converterId,
                ["settings"] = settings
            };
        }

        Activity.SetProperty(binding, propertyName);
        await RaiseActivityUpdatedAsync(Activity);
    }

    private async Task OnConverterSettingsChanged(JsonObject settings, OutputDescriptor outputDescriptor)
    {
        var propertyName = outputDescriptor.Name.Camelize();
        var binding = Activity.GetProperty<JsonObject>(propertyName);
        if (binding?["converter"] is not JsonObject converter)
            return;

        converter["settings"] = settings.DeepClone();
        Activity.SetProperty(binding, propertyName);
        await RaiseActivityUpdatedAsync(Activity);
    }

    private async Task LoadConvertersAsync(OutputDescriptor outputDescriptor, BindingTargetOption? target, bool clearIncompatibleConverter)
    {
        var state = GetConverterState(outputDescriptor);
        var cancellationToken = state.BeginRequest();
        var requestVersion = state.RequestVersion;

        if (target == null)
        {
            state.Reset();
            return;
        }

        try
        {
            var descriptors = await OutputConverterService.GetOutputConvertersAsync(outputDescriptor.TypeName, target.DeclaredTypeName, cancellationToken);
            if (requestVersion != state.RequestVersion)
                return;

            state.Descriptors = descriptors;
            state.IsAvailable = true;

            if (clearIncompatibleConverter && !IsCurrentConverterCompatible(outputDescriptor, state))
            {
                var propertyName = outputDescriptor.Name.Camelize();
                var binding = Activity.GetProperty<JsonObject>(propertyName);
                binding?.Remove("converter");
                if (binding != null)
                {
                    Activity.SetProperty(binding, propertyName);
                    await RaiseActivityUpdatedAsync(Activity);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (requestVersion != state.RequestVersion)
                return;

            state.Reset();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var state in _converterStates.Values)
            state.Dispose();
    }

    private OutputConverterState GetConverterState(OutputDescriptor outputDescriptor)
    {
        if (_converterStates.TryGetValue(outputDescriptor.Name, out var state))
            return state;

        state = new OutputConverterState();
        _converterStates[outputDescriptor.Name] = state;
        return state;
    }

    private string GetConverterDisplayName(string? converterId, OutputConverterState state)
    {
        if (string.IsNullOrWhiteSpace(converterId))
            return Localizer["None"];

        var descriptor = state.Descriptors.FirstOrDefault(x => x.Id == converterId);
        return Localizer[descriptor == null || string.IsNullOrWhiteSpace(descriptor.DisplayName) ? converterId : descriptor.DisplayName];
    }

    private string? GetConverterId(string propertyName) =>
        (Activity.GetProperty<JsonObject>(propertyName)?["converter"] as JsonObject)?["id"]?.GetValue<string>();

    private JsonObject? GetConverterSettings(string propertyName) =>
        (Activity.GetProperty<JsonObject>(propertyName)?["converter"] as JsonObject)?["settings"] as JsonObject;

    private bool IsCurrentConverterCompatible(OutputDescriptor outputDescriptor, OutputConverterState state)
    {
        var converterId = GetConverterId(outputDescriptor.Name.Camelize());
        return string.IsNullOrWhiteSpace(converterId) || state.Descriptors.Any(x => x.Id == converterId);
    }

    private async Task RaiseActivityUpdatedAsync(JsonObject activity)
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }

    private sealed class OutputConverterState : IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public ICollection<OutputConverterDescriptor> Descriptors { get; set; } = [];
        public bool IsAvailable { get; set; }
        public int RequestVersion { get; set; }

        public CancellationToken BeginRequest()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new();
            RequestVersion++;
            return _cancellationTokenSource.Token;
        }

        public void Reset()
        {
            Descriptors = [];
            IsAvailable = false;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}
