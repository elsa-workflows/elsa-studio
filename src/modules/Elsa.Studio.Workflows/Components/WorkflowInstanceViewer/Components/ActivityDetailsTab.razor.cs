using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the details of an activity.
public partial class ActivityDetailsTab
{
    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }
    
    /// The activity to display details for.
    [Parameter] public JsonObject Activity { get; set; } = null!;
    
    /// The latest activity execution record. Used for displaying the last state of the activity.
    [Parameter] public ActivityExecutionRecord? LastActivityExecution { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;

    private ActivityExecutionRecord? SelectedItem { get; set; } = null!;

    private DataPanelModel ActivityInfo { get; set; } = new();
    private DataPanelModel ActivityData { get; set; } = new();
    private DataPanelModel OutcomesData { get; set; } = new();
    private DataPanelModel OutputData { get; set; } = new();
    private DataPanelModel ExceptionData { get; set; } = new();
    private IDictionary<string, string?> SelectedActivityState { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutcomesData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutputData { get; set; } = new Dictionary<string, string?>();
    
    /// Refreshes the component.
    public void Refresh()
    {
        CreateDataModels();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        SelectedItem = null;
        CreateDataModels();
    }

    private void CreateDataModels()
    {
        var activity = Activity;
        var activityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion())!;
        var activityId = activity.GetId();
        var activityName = activity.GetName();
        var activityType = activity.GetTypeName();
        var execution = LastActivityExecution;
        var activityVersion = activity.GetVersion();
        var exception = execution?.Exception;
        var workflowDefinitionId = activity.GetIsWorkflowDefinitionActivity() ? activity.GetWorkflowDefinitionId() : null;

        var activityInfo = new DataPanelModel
        {
            new DataPanelItem("ID", activityId),
            new DataPanelItem("Name", activityName),
            new DataPanelItem("Type", activityType,
                string.IsNullOrWhiteSpace(workflowDefinitionId)
                    ? null
                    : $"/workflows/definitions/{workflowDefinitionId}/edit"),
            new DataPanelItem("Version", activityVersion.ToString())
        };

        var outcomesData = new DataPanelModel();
        var outputData = new DataPanelModel();

        if (execution != null)
        {
            activityInfo.Add("Status", execution.Status.ToString());
            activityInfo.Add("Instance ID", execution.Id);

            if (execution.Payload != null)
                if (execution.Payload.TryGetValue("Outcomes", out var outcomes))
                    outcomesData.Add("Outcomes", outcomes.ToString());

            var outputDescriptors = activityDescriptor.Outputs;
            var outputs = execution.Outputs;

            foreach (var outputDescriptor in outputDescriptors)
            {
                var outputValue = outputs != null
                    ? outputs.TryGetValue(outputDescriptor.Name, out var value) ? value : null
                    : null;
                outputData.Add(outputDescriptor.Name, outputValue?.ToString());
            }
        }
        else
        {
            activityInfo.Add("Status", "Not executed");
        }

        var exceptionData = new DataPanelModel();

        if (exception != null)
        {
            exceptionData.Add("Message", exception.Message);
            exceptionData.Add("InnerException", exception.InnerException != null
                ? exception.InnerException.Type + ": " + exception.InnerException.Message
                : null);
            exceptionData.Add("StackTrace", exception.StackTrace);
        }

        var activityStateData = new DataPanelModel();
        var activityState = execution?.ActivityState;

        if (activityState != null)
        {
            foreach (var inputDescriptor in activityDescriptor.Inputs)
            {
                var inputValue = activityState.TryGetValue(inputDescriptor.Name, out var value) ? value : null;
                activityStateData.Add(inputDescriptor.Name, inputValue?.ToString());
            }
        }

        ActivityInfo = activityInfo;
        ActivityData = activityStateData;
        OutcomesData = outcomesData;
        OutputData = outputData;
        ExceptionData = exceptionData;
    }

    private void CreateSelectedItemDataModels(ActivityExecutionRecord? record)
    {
        if (record == null)
        {
            SelectedActivityState = new Dictionary<string, string?>();
            SelectedOutcomesData = new Dictionary<string, string?>();
            SelectedOutputData = new Dictionary<string, string?>();
            return;
        }

        var activityState = record.ActivityState?
            .Where(x => !x.Key.StartsWith("_"))
            .ToDictionary(x => x.Key, x => x.Value?.ToString());

        var outcomesData = record.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new Dictionary<string, string?> { ["Outcomes"] = outcomesValue.ToString()! }
            : null;

        var outputData = new Dictionary<string, string?>();

        if (record?.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData[key] = value.ToString()!;

        SelectedActivityState = activityState ?? new Dictionary<string, string?>();
        SelectedOutcomesData = outcomesData ?? new Dictionary<string, string?>();
        SelectedOutputData = outputData;
    }
}