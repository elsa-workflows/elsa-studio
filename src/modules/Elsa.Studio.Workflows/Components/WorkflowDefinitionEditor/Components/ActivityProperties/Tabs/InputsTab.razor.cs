using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// A tab for editing the inputs of an activity.
/// </summary>
public partial class InputsTab
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    
    /// <summary>
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter] public JsonObject? Activity { get; set; }
    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }
    
    /// <summary>
    /// An event that is invoked when the activity is updated.
    /// </summary>
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    [Inject] private IUIHintService UIHintService { get; set; } = default!;
    [Inject] private ISyntaxService SyntaxService { get; set; } = default!;
    [Inject] private IRemoteBackendApiClientProvider RemoteBackendApiClientProvider { get; set; }= default!;
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

    private async Task<IEnumerable<ActivityInputDisplayModel>> BuildInputEditorModels(JsonObject activity, ActivityDescriptor activityDescriptor, IEnumerable<InputDescriptor> inputDescriptors)
    {
        var models = new List<ActivityInputDisplayModel>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            var wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : default;
            var syntaxProvider = wrappedInput != null ? SyntaxService.GetSyntaxProviderByExpressionType(wrappedInput.Expression.GetType()) : default;
            var uiHintHandler = UIHintService.GetHandler(inputDescriptor.UIHint);
            object? input = inputDescriptor.IsWrapped ? wrappedInput : value;

            if (inputDescriptor.Options != null)
                await RefreshOptions(activityDescriptor, inputDescriptor);

            var context = new DisplayInputEditorContext
            {
                WorkflowDefinition = WorkflowDefinition!,
                Activity = activity,
                ActivityDescriptor = activityDescriptor,
                InputDescriptor = inputDescriptor,
                Value = input,
                SelectedSyntaxProvider = syntaxProvider,
                UIHintHandler = uiHintHandler,
                IsReadOnly = Workspace?.IsReadOnly ?? false
            };

            context.OnValueChanged = async v => await HandleValueChangedAsync(context, v);
            var editor = uiHintHandler.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor));
        }

        return models;
    }

    private async Task RefreshOptions(ActivityDescriptor activityDescriptor, InputDescriptor inputDescriptor)
    {
        var activityTypeName = activityDescriptor.TypeName;
        var propertyName = inputDescriptor.Name;

        var api = await RemoteBackendApiClientProvider.GetApiAsync<IActivityDescriptorOptionsApi>();
        var options = await api.GetAsync(activityTypeName, propertyName,new GetActivityDescriptorOptionsRequest()
        {
            Context = new { Hello = "world 2" }
        });

        inputDescriptor.Options = options.Items;
    }

    private static WrappedInput? ToWrappedInput(object? value)
    {
        var converterOptions = new ObjectConverterOptions(serializerOptions =>
        {
            serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());
        });

        return value.ConvertTo<WrappedInput>(converterOptions);
    }

    private async Task HandleValueChangedAsync(DisplayInputEditorContext context, object? value)
    {
        var activity = context.Activity;
        var inputDescriptor = context.InputDescriptor;

        if (inputDescriptor.IsWrapped)
        {
            var wrappedInput = (WrappedInput)value!;
            var syntaxProvider = SyntaxService.GetSyntaxProviderByExpressionType(wrappedInput.Expression.GetType());
            context.SelectedSyntaxProvider = syntaxProvider;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new ExpressionJsonConverterFactory());

        var propName = inputDescriptor.Name.Camelize();
        activity.SetProperty(value?.SerializeToNode(options), propName);

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}