using System.Text.Json;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties.Tabs;

public partial class InputsTab
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public Func<Activity, Task>? OnActivityUpdated { get; set; }
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IUIHintService UIHintService { get; set; } = default!;
    [Inject] private ISyntaxService SyntaxService { get; set; } = default!;

    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private ICollection<ActivityInputDisplayModel> InputDisplayModels { get; set; } = new List<ActivityInputDisplayModel>();

    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null)
            return;

        ActivityDescriptor = await ActivityRegistry.FindAsync(Activity.Type);

        if (ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs;
        OutputDescriptors = ActivityDescriptor.Outputs;
        InputDisplayModels = BuildInputEditorModels(Activity, ActivityDescriptor, InputDescriptors).ToList();
    }

    private IEnumerable<ActivityInputDisplayModel> BuildInputEditorModels(Activity activity, ActivityDescriptor activityDescriptor, IEnumerable<InputDescriptor> inputDescriptors)
    {
        var models = new List<ActivityInputDisplayModel>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.TryGetValue(inputName);
            var wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : default;
            var syntaxProvider = wrappedInput != null ? SyntaxService.GetSyntaxProviderByExpressionType(wrappedInput.Expression.GetType()) : default;
            var uiHintHandler = UIHintService.GetHandler(inputDescriptor.UIHint);
            var input = inputDescriptor.IsWrapped ? wrappedInput : value;

            var context = new DisplayInputEditorContext
            {
                Activity = activity,
                ActivityDescriptor = activityDescriptor,
                InputDescriptor = inputDescriptor,
                Value = input,
                SelectedSyntaxProvider = syntaxProvider,
                UIHintHandler = uiHintHandler,
            };

            context.OnValueChanged = async v => await HandleValueChangedAsync(context, v);
            var editor = uiHintHandler.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor));
        }

        return models;
    }

    private WrappedInput? ToWrappedInput(object? value)
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

        activity[inputDescriptor.Name.Camelize()] = value!;

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }
}