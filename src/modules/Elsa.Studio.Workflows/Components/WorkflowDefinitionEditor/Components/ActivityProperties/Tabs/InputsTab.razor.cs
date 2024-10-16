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

    [Inject] private IRemoteBackendApiClientProvider RemoteBackendApiClientProvider { get; set; } = default!;
    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private ICollection<ActivityInputDisplayModel> InputDisplayModels { get; set; } = new List<ActivityInputDisplayModel>();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();
        InputDisplayModels = (await BuildInputEditorModels(Activity, ActivityDescriptor, InputDescriptors)).ToList();
    }

    private async Task<IEnumerable<ActivityInputDisplayModel>> BuildInputEditorModels(JsonObject activity, ActivityDescriptor activityDescriptor, ICollection<InputDescriptor> inputDescriptors)
    {
        var models = new List<ActivityInputDisplayModel>();
        var browsableInputDescriptors = inputDescriptors.Where(x => x.IsBrowsable == true).ToList();

        foreach (var inputDescriptor in browsableInputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            var wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : default;
            var syntaxProvider = wrappedInput != null ? ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type) : default;

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
                IsReadOnly = (Workspace?.IsReadOnly ?? false) || (inputDescriptor.IsReadOnly ?? false),
            };

            context.OnValueChanged = async v => await HandleValueChangedAsync(context, v);
            var editor = uiHintHandler.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor));
        }

        return models;
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

        var api = await RemoteBackendApiClientProvider.GetApiAsync<IActivityDescriptorOptionsApi>();

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