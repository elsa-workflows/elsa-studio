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

    /// <summary>
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
    private static Dictionary<string, JsonDocument> _InputDescriptors = new();
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
            JsonDocument? InputDescriptor = null;
            if (inputDescriptor.Description is not null)
            {
                SetInputDescriptor(inputDescriptor);
                InputDescriptor = GetInputDescriptor(inputDescriptor);

                if (InputDescriptor is not null)
                {
                    string? inputType = InputDescriptor.RootElement.GetProperty("InputType").GetString();
                    // Check if we have conditional inputs
                    if (inputType == "StateDropdown")
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
                    else if (inputType == "ConditionalInput")
                    {
                        inputDescriptor.Description = InputDescriptor.RootElement.GetProperty("Description").EnumerateArray().First().GetString();
                    }
                    else
                    {
                        inputDescriptor.Description = InputDescriptor.RootElement.GetProperty("Description").GetString();
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

    private string GetInputDescriptorKey(InputDescriptor inputDescriptor)
    {
        return ActivityDescriptor?.Name + inputDescriptor.Name;
    }

    private JsonDocument? GetInputDescriptor(InputDescriptor inputDescriptor)
    {
        return _InputDescriptors.GetValueOrDefault(GetInputDescriptorKey(inputDescriptor));
    }

    private void SetInputDescriptor(InputDescriptor inputDescriptor)
    {
        try
        {
            var InputDescriptor = JsonDocument.Parse(inputDescriptor.Description!);
            _InputDescriptors[GetInputDescriptorKey(inputDescriptor)] = InputDescriptor;
        }
        catch
        {
            // Ignore --> Standard Elsa 3 descriptor
        }
    }

    private void AddSelectedState(InputDescriptor inputDescriptor, WrappedInput? v)
    {
        var InputDescriptor = GetInputDescriptor(inputDescriptor);
        if (v is null || InputDescriptor is null) return;

        var valueAsString = v!.Expression.ToString();
        AddSelectedState(inputDescriptor, valueAsString);
        UpdateDescription(inputDescriptor, v);
    }

    private void AddSelectedState(InputDescriptor inputDescriptor, object? value)
    {
        if (value is null) return;

        var InputDescriptor = GetInputDescriptor(inputDescriptor);
        if (InputDescriptor is null) return;

        var inputDescriptorKey = GetInputDescriptorKey(inputDescriptor);
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

        var stateNames = InputDescriptor.RootElement.GetProperty("Options").EnumerateArray();
        var stateIds = InputDescriptor.RootElement.GetProperty("Ids").EnumerateArray();

        for (var i = 0; i < stateNames.Count(); ++i)
        {
            var current = stateNames.ElementAt(i);
            if (current.GetString() == valueAsString)
            {
                var id = stateIds.ElementAt(i).GetString()!;

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

            var InputDescriptor = GetInputDescriptor(inputDescriptor);
            // Check if we have a custom input descriptor
            if (InputDescriptor is null)
            {
                return;
            }

            // Update the selected states
            if (InputDescriptor.RootElement.GetProperty("InputType").GetString() == "StateDropdown")
            {
                AddSelectedState(inputDescriptor, v as WrappedInput);
            }
        };
    }

    public void UpdateDescription(InputDescriptor inputDescriptor, object? value)
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

        var InputDescriptor = GetInputDescriptor(inputDescriptor)!;
        // Update with the correct description
        var options = InputDescriptor.RootElement.GetProperty("Options").EnumerateArray();
        var descriptions = InputDescriptor.RootElement.GetProperty("Description").EnumerateArray();
        var foundDescription = false;
        for (var i = 0; i < options.Count(); ++i)
        {
            if (options.ElementAt(i).GetString() == valueAsString)
            {
                inputDescriptor.Description = descriptions.ElementAt(i).GetString();
                foundDescription = true;
                break;
            }
        }

        if (!foundDescription)
        {
            inputDescriptor.Description = "";
        }
    }

    public bool IsVisibleInput(ActivityInputDisplayModel model)
    {

        var InputDescriptor = GetInputDescriptor(model.InputDescriptor);

        if (InputDescriptor == null) return true;

        // Check we have a match for the selected state
        if (InputDescriptor.RootElement.GetProperty("InputType").GetString() == "ConditionalInput")
        {
            var validStates = InputDescriptor.RootElement.GetProperty("ShowForStates");
            foreach (var validState in validStates.EnumerateArray())
            {
                var value = validState.GetString();
                if (value is not null && _selectedStates.Contains(value)) return true;
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