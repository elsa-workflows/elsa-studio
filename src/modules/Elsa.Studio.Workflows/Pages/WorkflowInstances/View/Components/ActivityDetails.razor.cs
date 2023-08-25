using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

/// <summary>
/// Displays the details of an activity.
/// </summary>
public partial class ActivityDetails
{
    /// <summary>
    /// Represents a row in the table of activity executions.
    /// </summary>
    /// <param name="Number">The number of executions.</param>
    /// <param name="ActivityExecution">The activity execution.</param>
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecord ActivityExecution);

    /// <summary>
    /// The height of the visible pane.
    /// </summary>
    [Parameter]
    public int VisiblePaneHeight { get; set; }

    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter]
    public JsonObject Activity { get; set; } = default!;

    /// <summary>
    /// The activity execution records.
    /// </summary>
    [Parameter]
    public ICollection<ActivityExecutionRecord> ActivityExecutions { get; set; } = new List<ActivityExecutionRecord>();

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;

    private ActivityExecutionRecord? LastActivityExecution => ActivityExecutions.LastOrDefault();
    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutions.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; } = default!;

    private IDictionary<string, string?> ActivityInfo { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> ActivityData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> OutcomesData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> OutputData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> ExceptionData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedActivityState { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutcomesData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutputData { get; set; } = new Dictionary<string, string?>();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        SelectedItem = null;
        CreateDataModels();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
    }

    private void CreateDataModels()
    {
        var activity = Activity;
        var activityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion())!;
        var activityId = activity.GetId();
        var activityName = activity.GetName();
        var activityType = activity.GetTypeName();
        var execution = LastActivityExecution;
        var activityVersion = execution?.ActivityTypeVersion;
        var exception = execution?.Exception;

        var activityInfo = new Dictionary<string, string?>
        {
            ["ID"] = activityId,
            ["Name"] = activityName,
            ["Type"] = activityType,
            ["Version"] = activityVersion.ToString()
        };

        var outcomesData = new Dictionary<string, string?>();
        var outputData = new Dictionary<string, string?>();

        if (execution != null)
        {
            activityInfo["Status"] = execution.Status.ToString();
            activityInfo["Instance ID"] = execution.Id;

            if (execution.Payload != null)
                if (execution.Payload.TryGetValue("Outcomes", out var outcomes))
                    outcomesData["Outcomes"] = outcomes.ToString();

            var outputDescriptors = activityDescriptor.Outputs;
            var outputs = execution.Outputs;

            foreach (var outputDescriptor in outputDescriptors)
            {
                var outputValue = outputs != null ? outputs.TryGetValue(outputDescriptor.Name, out var value) ? value : default : default;
                outputData[outputDescriptor.Name] = outputValue?.ToString();
            }
        }

        var exceptionData = new Dictionary<string, string?>();

        if (exception != null)
        {
            exceptionData["Message"] = exception.Message;
            exceptionData["InnerException"] = exception.InnerException != null ? exception.InnerException.Type + ": " + exception.InnerException.Message : default;
            exceptionData["StackTrace"] = exception.StackTrace;
        }

        var activityStateData = new Dictionary<string, string?>();
        var activityState = execution?.ActivityState;

        if (activityState != null)
        {
            foreach (var inputDescriptor in activityDescriptor.Inputs)
            {
                var inputValue = activityState.TryGetValue(inputDescriptor.Name, out var value) ? value : default;
                activityStateData[inputDescriptor.Name] = inputValue?.ToString();
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
            .ToDictionary(x => x.Key, x => x.Value.ToString());

        var outcomesData = record.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new Dictionary<string, string?> { ["Outcomes"] = outcomesValue.ToString()! }
            : default;

        var outputData = new Dictionary<string, string?>();

        if (record?.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData[key] = value.ToString()!;

        SelectedActivityState = activityState ?? new Dictionary<string, string?>();
        SelectedOutcomesData = outcomesData ?? new Dictionary<string, string?>();
        SelectedOutputData = outputData;
    }

    private Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        SelectedItem = arg.Item.ActivityExecution;
        CreateSelectedItemDataModels(SelectedItem);
        StateHasChanged();
        return Task.CompletedTask;
    }
}