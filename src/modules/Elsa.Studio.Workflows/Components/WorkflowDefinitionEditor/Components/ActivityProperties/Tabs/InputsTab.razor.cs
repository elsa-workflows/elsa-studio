using System.Text.Json;
using System.Text.Json.Nodes;

using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Responses;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
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

    private Dictionary<string, string> _selectedStates = [];
    private static Dictionary<string, ConditionalDescriptor> _conditionalDescriptors = new();

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
        List<ActivityInputDisplayModel> models = [];

        foreach (InputDescriptor inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            JsonNode? value = activity.GetProperty(inputName);
            WrappedInput? wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : null;

            // Check if we have a custom  input
            if (inputDescriptor.ConditionalDescriptor is not null)
            {
                SetConditionalDescriptor(inputDescriptor);
                ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);

                InputType? inputType = conditionalDescriptor?.InputType;
                if (inputType == InputType.StateDropdown)
                {
                    if (wrappedInput is not null)
                    {
                        UpdateSelectState(inputDescriptor, wrappedInput);
                        //UpdateDescription(inputDescriptor, wrappedInput);
                    }
                    else if (wrappedInput is null && inputDescriptor.DefaultValue is not null)
                    {
                        // Add the default value to the selected states
                        UpdateSelectState(inputDescriptor, inputDescriptor.DefaultValue);
                        //UpdateDescription(inputDescriptor, inputDescriptor.DefaultValue);
                    }
                    // else if (wrappedInput is null || (string.IsNullOrEmpty(wrappedInput.Expression.ToString())))
                    // {
                    //     // Empty state && InputDescriptor exists, therefore we hide the description
                    //     inputDescriptor.Description = "";
                    // }
                }
            }

            // Check if refresh is needed.
            if (inputDescriptor.UISpecifications != null
                && inputDescriptor.UISpecifications.TryGetValue("Refresh", out var refreshInput)
                && bool.Parse(refreshInput.ToString()!))
            {
                Task? task = _rateLimitedInputPropertyRefreshAsync.Invoke(activity, activityDescriptor, inputDescriptors, inputDescriptor);
                if (task != null)
                    await task;
            }

            IUIHintHandler uiHintHandler = UIHintService.GetHandler(inputDescriptor.UIHint);
            var input = inputDescriptor.IsWrapped ? wrappedInput : (object?)value;
            ExpressionDescriptor? syntaxProvider = wrappedInput != null ? ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type) : null;

            DisplayInputEditorContext context = new()
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

            context.OnValueChanged = CreateHandleValueChangedCallback(context, inputDescriptor);
            RenderFragment editor = uiHintHandler.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor, inputDescriptor));
        }
        return models;
    }

    private string CreateConditionalDescriptorKey(InputDescriptor inputDescriptor)
    {
        return ActivityDescriptor?.Name + inputDescriptor.Name;
    }

    private ConditionalDescriptor? GetConditionalDescriptor(InputDescriptor inputDescriptor)
    {
        return _conditionalDescriptors.GetValueOrDefault(CreateConditionalDescriptorKey(inputDescriptor));
    }

    private void SetConditionalDescriptor(InputDescriptor inputDescriptor)
    {
        ConditionalDescriptor? cond = inputDescriptor.ConditionalDescriptor;
        if (cond is null) return;
        
        var key = CreateConditionalDescriptorKey(inputDescriptor);
        _conditionalDescriptors[key] = cond;
    }

    private void UpdateSelectState(InputDescriptor inputDescriptor, WrappedInput? wrappedInput)
    {
        if (wrappedInput is null) return;
        ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);
        if (conditionalDescriptor is null) return;

        var valueAsString = wrappedInput.Expression.ToString();
        UpdateSelectState(inputDescriptor, valueAsString);
        //UpdateDescription(inputDescriptor, wrappedInput);
    }

    private void UpdateSelectState(InputDescriptor inputDescriptor, object? value)
    {
        ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);
        if (conditionalDescriptor is null) return;

        var descriptorKey = CreateConditionalDescriptorKey(inputDescriptor);
        
        // Remove old state
        _selectedStates.Remove(descriptorKey);
        
        var valueAsString = value as string;
        if (value is JsonElement jsonElement)
        {
            valueAsString = jsonElement.GetString();
        }
        if (valueAsString is null) return;
        
        List<string> stateValues = conditionalDescriptor.DropDownStates!.Options;
        List<string> stateIds = conditionalDescriptor.DropDownStates.Ids;
        
        var valueIndex = stateValues.IndexOf(valueAsString);
        var id = stateIds[valueIndex];
        _selectedStates.Add(descriptorKey, id);
        StateHasChanged();
    }

    private  Func<object?, Task> CreateHandleValueChangedCallback(DisplayInputEditorContext context, InputDescriptor inputDescriptor)
    {
        return async value =>
        {
            await HandleValueChangedAsync(context, value);

            ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(inputDescriptor);
            if (conditionalDescriptor is null)
                return;
            
            if (conditionalDescriptor.InputType == InputType.StateDropdown)
                UpdateSelectState(inputDescriptor, value as WrappedInput);
        };
    }

    // private void UpdateDescription(InputDescriptor inputDescriptor, object? value)
    // {
    //     if (value is null) return;

    //     var valueAsString = value switch
    //     {
    //         WrappedInput wrappedInput => wrappedInput.Expression.ToString(),
    //         JsonElement jsonElement => jsonElement.GetString(),
    //         _ => ""
    //     };

    //     ConditionalDescriptor conditionalDescriptor = GetConditionalDescriptor(inputDescriptor)!;

    //     List<string> options = conditionalDescriptor.DropDownStates!.Options;
    //     List<string> descriptions = conditionalDescriptor.DropDownStates.Descriptions;
    //     var foundDescription = false;
    //     for (var i = 0; i < options.Count(); ++i)
    //     {
    //         if (options.ElementAt(i) == valueAsString)
    //         {
    //             inputDescriptor.Description = descriptions.ElementAt(i);
    //             foundDescription = true;
    //             break;
    //         }
    //     }

    //     if (!foundDescription)
    //     {
    //         inputDescriptor.Description = "";
    //     }
    // }

    private bool IsVisibleInput(ActivityInputDisplayModel model)
    {
        ConditionalDescriptor? conditionalDescriptor = GetConditionalDescriptor(model.InputDescriptor);
        // If the input has no conditional input, it's a normal input and
        // has to be shown
        if (conditionalDescriptor == null) return true;

        // Always show normal inputs
        if (conditionalDescriptor.InputType != InputType.ConditionalInput)
            return true;

        var validStates = conditionalDescriptor.ShowForStates;
        return validStates.Any(x => _selectedStates.ContainsValue(x));     
    }

    private async Task RefreshDescriptor(JsonObject activity, ActivityDescriptor activityDescriptor, IEnumerable<InputDescriptor> inputDescriptors, InputDescriptor currentInputDescriptor)
    {
        var activityTypeName = activityDescriptor.TypeName;
        var propertyName = currentInputDescriptor.Name;

        // Embed all props value in the context.
        Dictionary<string, object> contextDictionary = new Dictionary<string, object>();
        foreach (var inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            if (value != null)
                contextDictionary.Add(inputName, value);
        }

        IActivityDescriptorOptionsApi api = await BackendApiClientProvider.GetApiAsync<IActivityDescriptorOptionsApi>();
        GetActivityDescriptorOptionsResponse result = await api.GetAsync(activityTypeName, propertyName, new GetActivityDescriptorOptionsRequest()
        {
            Context = contextDictionary
        });
        currentInputDescriptor.UISpecifications = result.Items;
    }

    private static WrappedInput? ToWrappedInput(object? value)
    {
        ObjectConverterOptions converterOptions = new ObjectConverterOptions(serializerOptions => { serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });
        return value.ConvertTo<WrappedInput>(converterOptions);
    }

    private async Task HandleValueChangedAsync(DisplayInputEditorContext context, object? value)
    {
        JsonObject activity = context.Activity;
        InputDescriptor inputDescriptor = context.InputDescriptor;

        if (inputDescriptor.IsWrapped)
        {
            WrappedInput wrappedInput = (WrappedInput)value!;
            ExpressionDescriptor? syntaxProvider = ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type);
            context.SelectedExpressionDescriptor = syntaxProvider;
        }

        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var propName = inputDescriptor.Name.Camelize();
        activity.SetProperty(value?.SerializeToNode(options), propName);

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}