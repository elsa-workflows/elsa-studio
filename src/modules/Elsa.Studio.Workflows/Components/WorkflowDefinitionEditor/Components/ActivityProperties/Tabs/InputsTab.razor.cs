using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// A tab for editing the inputs of an activity.
/// </summary>
public partial class InputsTab
{
    private readonly RateLimitedFunc<JsonObject, ActivityDescriptor, IEnumerable<InputDescriptor>, InputDescriptor, Task> _rateLimitedInputPropertyRefreshAsync;

    /// <inheritdoc />
    public InputsTab()
    {
        _rateLimitedInputPropertyRefreshAsync = Debouncer.Debounce<JsonObject, ActivityDescriptor, IEnumerable<InputDescriptor>, InputDescriptor, Task>(RefreshDescriptor, TimeSpan.FromMilliseconds(100), true);
    }

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter]
    public JsonObject? Activity { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor? ActivityDescriptor { get; set; }

    /// <summary>
    /// An event that is invoked when the activity is updated.
    /// </summary>
    [Parameter]
    public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    [CascadingParameter] private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = default!;
    [Inject] private IUIHintService UIHintService { get; set; } = default!;

    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;
    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private ICollection<ActivityInputDisplayModel> InputDisplayModels { get; set; } = new List<ActivityInputDisplayModel>();

    private List<string> _selectedStates = [];
    private static Dictionary<string, ConditionalDescriptor> _inputDescriptors = new();
    private static Dictionary<string, string> _previousStates = new();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();
        InputDisplayModels = (await BuildInputEditorModels(Activity, ActivityDescriptor, InputDescriptors)).ToList();
        StateHasChanged();
    }

    private async Task<IEnumerable<ActivityInputDisplayModel>> BuildInputEditorModels(JsonObject activity, ActivityDescriptor activityDescriptor, ICollection<InputDescriptor> inputDescriptors)
    {
        var models = new List<ActivityInputDisplayModel>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            var wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : default;
            var syntaxProvider = wrappedInput != null ? ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type) : default;

            // Check if we have a custom  input
            if (inputDescriptor.ConditionalDescriptor is not null)
            {
                SetConditionalDescriptor(inputDescriptor);
                ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);

                if (conditionalDescriptor is not null)
                {
                    InputType inputType = conditionalDescriptor.InputType;
                    // Check if we have conditional inputs
                    if (inputType == InputType.StateDropdown)
                    {
                        if (wrappedInput is not null)
                        {
                            // Add the current value to the selected states
                            AddSelectedState(inputDescriptor, wrappedInput);
                            UpdateDescription(inputDescriptor, wrappedInput);
                        }
                        else if (wrappedInput is null && inputDescriptor.DefaultValue is not null)
                        {
                            // Add the default value to the selected states
                            AddSelectedState(inputDescriptor, inputDescriptor.DefaultValue);
                            UpdateDescription(inputDescriptor, inputDescriptor.DefaultValue);
                        }
                        else if (wrappedInput is null || (wrappedInput is not null && string.IsNullOrEmpty(wrappedInput.Expression.ToString())))
                        {
                            // Empty state && InputDescriptor exists, therefore we hide the description
                            inputDescriptor.Description = "";
                        }
                    }
                }
            }

            // Check if refresh is needed.
            if (inputDescriptor.UISpecifications != null
                && inputDescriptor.UISpecifications.TryGetValue("Refresh", out var refreshInput)
                && bool.Parse(refreshInput.ToString()!))
            {
                var task = _rateLimitedInputPropertyRefreshAsync.Invoke(activity, activityDescriptor, inputDescriptors, inputDescriptor);
                if (task != null)
                    await task;
            }

            var uiHintHandler = UIHintService.GetHandler(inputDescriptor.UIHint);
            var input = inputDescriptor.IsWrapped ? wrappedInput : (object?)value;

            var context = new DisplayInputEditorContext
            {
                WorkflowDefinition = WorkflowDefinition!,
                Activity = activity,
                ActivityDescriptor = activityDescriptor,
                InputDescriptor = inputDescriptor,
                Value = input,
                SelectedExpressionDescriptor = syntaxProvider,
                UIHintHandler = uiHintHandler,
                IsReadOnly = Workspace?.IsReadOnly ?? false
            };

            context.OnValueChanged = HandleValueChangedAsync(context, inputDescriptor);
            var editor = uiHintHandler.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor, inputDescriptor));
        }

        return models;
    }

    private string GetConditionalDescriptorKey(InputDescriptor inputDescriptor)
    {
        return ActivityDescriptor?.Name + inputDescriptor.Name;
    }

    private ConditionalDescriptor? GetConditionalDescriptor(InputDescriptor inputDescriptor)
    {
        return _inputDescriptors.GetValueOrDefault(GetConditionalDescriptorKey(inputDescriptor));
    }

    private void SetConditionalDescriptor(InputDescriptor inputDescriptor)
    {
        try
        {
            var cond = inputDescriptor.ConditionalDescriptor;
            _inputDescriptors[GetConditionalDescriptorKey(inputDescriptor)] = cond;
        }
        catch
        {
            // Ignore --> Standard Elsa 3 descriptor
        }
    }

    private void AddSelectedState(InputDescriptor inputDescriptor, WrappedInput? v)
    {
        var InputDescriptor = GetConditionalDescriptor(inputDescriptor);
        if (v is null || InputDescriptor is null) return;

        var valueAsString = v!.Expression.ToString();
        AddSelectedState(inputDescriptor, valueAsString);
        UpdateDescription(inputDescriptor, v);
    }

    private void AddSelectedState(InputDescriptor inputDescriptor, object? value)
    {
        if (value is null) return;

        ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);
        if (conditionalDescriptor is null) return;

        var inputDescriptorKey = GetConditionalDescriptorKey(inputDescriptor);
        // Remove the previous state
        var previousState = _previousStates.GetValueOrDefault(inputDescriptorKey, string.Empty);
        if (!string.IsNullOrEmpty(previousState))
        {
            RemoveSelectedState(previousState);
        }

        var valueAsString = value as string;
        if (value is JsonElement)
        {
            valueAsString = ((JsonElement)value).GetString();
        }

        var stateNames = conditionalDescriptor.DropDownStates!.Options;
        var stateIds = conditionalDescriptor.DropDownStates.Ids;

        for (var i = 0; i < stateNames.Count(); ++i)
        {
            var current = stateNames.ElementAt(i);
            if (current == valueAsString)
            {
                var id = stateIds.ElementAt(i);

                // Ensure that we have no duplicates
                RemoveSelectedState(id);

                _selectedStates.Add(id);
                _previousStates[inputDescriptorKey] = id;
                break;
            }
        }

        StateHasChanged();
    }

    private void RemoveSelectedState(string name)
    {
        _selectedStates.Remove(name);
    }

    private Func<object?, Task> HandleValueChangedAsync(DisplayInputEditorContext context, InputDescriptor inputDescriptor)
    {
        return async v =>
        {
            await HandleValueChangedAsync(context, v);

            ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);
            // Check if we have a custom input descriptor
            if (conditionalDescriptor is null)
                return;

            // Update the selected states
            if (conditionalDescriptor.InputType == InputType.StateDropdown)
            {
                AddSelectedState(inputDescriptor, v as WrappedInput);
            }
        };
    }

    private void UpdateDescription(InputDescriptor inputDescriptor, object? value)
    {
        if (value is null)
        {
            return;
        }

        var valueAsString = "";
        if (value is WrappedInput)
        {
            valueAsString = ((WrappedInput)value).Expression.ToString();
        }
        else if (value is JsonElement)
        {
            valueAsString = ((JsonElement)value).GetString();
        }

        ConditionalDescriptor conditionalDescriptor = GetConditionalDescriptor(inputDescriptor)!;
        // Update with the correct description
        var options = conditionalDescriptor.DropDownStates.Options;
        var descriptions = conditionalDescriptor.DropDownStates.Descriptions;
        var foundDescription = false;
        for (var i = 0; i < options.Count(); ++i)
        {
            if (options.ElementAt(i) == valueAsString)
            {
                inputDescriptor.Description = descriptions.ElementAt(i);
                foundDescription = true;
                break;
            }
        }

        if (!foundDescription)
        {
            inputDescriptor.Description = "";
        }
    }

    private bool IsVisibleInput(ActivityInputDisplayModel model)
    {
        ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(model.InputDescriptor);

        if (conditionalDescriptor == null) return true;

        // Check we have a match for the selected state
        if (conditionalDescriptor.InputType == InputType.ConditionalInput)
        {
            var validStates = conditionalDescriptor.ShowForStates;
            foreach (var validState in validStates)
            {
                if (_selectedStates.Contains(validState)) return true;
            }
            return false;
        }

        return true;
    }

    private async Task RefreshDescriptor(JsonObject activity, ActivityDescriptor activityDescriptor, IEnumerable<InputDescriptor> inputDescriptors, InputDescriptor currentInputDescriptor)
    {
        var activityTypeName = activityDescriptor.TypeName;
        var propertyName = currentInputDescriptor.Name;

        // Embed all props value in the context.
        var contextDictionary = new Dictionary<string, object>();
        foreach (var inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            if (value != null)
                contextDictionary.Add(inputName, value);
        }

        var api = await BackendApiClientProvider.GetApiAsync<IActivityDescriptorOptionsApi>();

        var result = await api.GetAsync(activityTypeName, propertyName, new GetActivityDescriptorOptionsRequest()
        {
            Context = contextDictionary
        });

        currentInputDescriptor.UISpecifications = result.Items;
    }

    private static WrappedInput? ToWrappedInput(object? value)
    {
        var converterOptions = new ObjectConverterOptions(serializerOptions => { serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

        return value.ConvertTo<WrappedInput>(converterOptions);
    }

    private async Task HandleValueChangedAsync(DisplayInputEditorContext context, object? value)
    {
        var activity = context.Activity;
        var inputDescriptor = context.InputDescriptor;

        if (inputDescriptor.IsWrapped)
        {
            var wrappedInput = (WrappedInput)value!;
            var syntaxProvider = ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type);
            context.SelectedExpressionDescriptor = syntaxProvider;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var propName = inputDescriptor.Name.Camelize();
        activity.SetProperty(value?.SerializeToNode(options), propName);

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}