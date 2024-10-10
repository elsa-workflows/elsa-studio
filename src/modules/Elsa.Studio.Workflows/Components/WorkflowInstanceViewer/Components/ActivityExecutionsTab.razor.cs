using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the details of an activity.
public partial class ActivityExecutionsTab : IAsyncDisposable
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
    private Timer? _refreshTimer;

    /// Refreshes the component.
    public async Task RefreshAsync()
    {
        await StopRefreshTimerAsync();
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

    private async Task RefreshSelectedItemAsync(string id)
    {
        var fullRecord = await InvokeWithBlazorServiceContext(() => ActivityExecutionService.GetAsync(id));
        SelectedItem = fullRecord;
        CreateSelectedItemDataModels(SelectedItem);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        await StopRefreshTimerAsync();
        var id = arg.Item.ActivityExecution.Id;
        await RefreshSelectedItemAsync(id);

        if (SelectedItem == null)
            return;

        // Check if the selected item has all of its details or not.
        // If not, periodically check for the details until they are available.
        if (SelectedItem.IsFused())
            return;

        // Start a timer to periodically check for the details.
        RefreshSelectedItemPeriodically(id);
    }

    private void RefreshSelectedItemPeriodically(string id)
    {
        async void Callback(object? _)
        {
            await RefreshSelectedItemAsync(id);

            if (SelectedItem == null || SelectedItem.IsFused()) 
                await StopRefreshTimerAsync();
        }

        _refreshTimer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private async Task StopRefreshTimerAsync()
    {
        if (_refreshTimer == null) return;
        await _refreshTimer.DisposeAsync();
        _refreshTimer = null;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_refreshTimer != null) await _refreshTimer.DisposeAsync();
    }
}