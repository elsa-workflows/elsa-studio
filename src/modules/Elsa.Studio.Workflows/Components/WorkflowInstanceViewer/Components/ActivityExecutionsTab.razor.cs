using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the details of an activity.
public partial class ActivityExecutionsTab
{
    /// Represents a row in the table of activity executions.
    /// <param name="Number">The number of executions.</param>
    /// <param name="ActivityExecution">The activity execution.</param>
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecordSummary ActivityExecution);

    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }

    /// The activity to display executions for.
    [Parameter] public JsonObject Activity { get; set; } = default!;

    /// The activity execution records.
    [Parameter] public ICollection<ActivityExecutionRecordSummary> ActivityExecutions { get; set; } = new List<ActivityExecutionRecordSummary>();
    
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;

    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutions.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; } = default!;
    private IDictionary<string, DataPanelItem> SelectedActivityState { get; set; } = new Dictionary<string, DataPanelItem>();
    private IDictionary<string, DataPanelItem> SelectedOutcomesData { get; set; } = new Dictionary<string, DataPanelItem>();
    private IDictionary<string, DataPanelItem> SelectedOutputData { get; set; } = new Dictionary<string, DataPanelItem>();

    /// Refreshes the component.
    public void Refresh()
    {
        SelectedItem = null;
        SelectedActivityState = new Dictionary<string, DataPanelItem>();
        SelectedOutcomesData = new Dictionary<string, DataPanelItem>();
        SelectedOutputData = new Dictionary<string, DataPanelItem>();
    }

    private void CreateSelectedItemDataModels(ActivityExecutionRecord? record)
    {
        if (record == null)
        {
            SelectedActivityState = new Dictionary<string, DataPanelItem>();
            SelectedOutcomesData = new Dictionary<string, DataPanelItem>();
            SelectedOutputData = new Dictionary<string, DataPanelItem>();
            return;
        }

        var activityState = record.ActivityState?
            .Where(x => !x.Key.StartsWith("_"))
            .ToDictionary(x => x.Key, x => new DataPanelItem(x.Value?.ToString()));

        var outcomesData = record.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new Dictionary<string, DataPanelItem> { ["Outcomes"] = new(outcomesValue!.ToString()!) }
            : default;

        var outputData = new Dictionary<string, DataPanelItem>();

        if (record.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData[key] = new(value?.ToString());

        SelectedActivityState = activityState ?? new Dictionary<string, DataPanelItem>();
        SelectedOutcomesData = outcomesData ?? new Dictionary<string, DataPanelItem>();
        SelectedOutputData = outputData;
    }

    private async Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        var id = arg.Item.ActivityExecution.Id;
        var fullRecord = await ActivityExecutionService.GetAsync(id);
        SelectedItem = fullRecord;
        CreateSelectedItemDataModels(SelectedItem);
        StateHasChanged();
    }
}