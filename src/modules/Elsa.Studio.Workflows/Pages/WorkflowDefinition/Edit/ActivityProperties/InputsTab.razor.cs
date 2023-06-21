using System.Text.Json;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Core.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public partial class InputsTab
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public EventCallback<Activity> OnActivityUpdated { get; set; }
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IUIHintService UIHintService { get; set; } = default!;

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
            var activityInput = ToActivityInput(value);
            var valueChangedCallback = EventCallback.Factory.Create<ActivityInput>(this, v => HandleValueChangedAsync(activity, inputDescriptor, v));
            var context = new DisplayInputEditorContext(activity, activityDescriptor, inputDescriptor, activityInput, valueChangedCallback);
            var editor = UIHintService.DisplayInputEditor(context);
            models.Add(new ActivityInputDisplayModel(editor));
        }

        return models;
    }

    private ActivityInput? ToActivityInput(object? value)
    {
        var converterOptions = new ObjectConverterOptions(serializerOptions =>
        {
            serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());
        });

        return value.ConvertTo<ActivityInput>(converterOptions);
    }

    private async Task HandleValueChangedAsync(Activity activity, InputDescriptor inputDescriptor, ActivityInput activityInput)
    {
        activity[inputDescriptor.Name.Camelize()] = activityInput;
        await OnActivityUpdated.InvokeAsync(activity);
    }
}