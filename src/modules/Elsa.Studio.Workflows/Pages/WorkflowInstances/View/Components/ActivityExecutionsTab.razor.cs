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
public partial class ActivityExecutionsTab
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
    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutions.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; } = default!;
    private IDictionary<string, string?> SelectedActivityState { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutcomesData { get; set; } = new Dictionary<string, string?>();
    private IDictionary<string, string?> SelectedOutputData { get; set; } = new Dictionary<string, string?>();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        SelectedItem = null;
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
            ? new Dictionary<string, string?> { ["Outcomes"] = outcomesValue!.ToString()! }
            : default;

        var outputData = new Dictionary<string, string?>();

        if (record?.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData[key] = value?.ToString();

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