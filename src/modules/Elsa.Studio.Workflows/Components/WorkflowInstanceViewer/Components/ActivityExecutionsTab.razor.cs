using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
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
    /// <param name="ActivityExecutionSummary">The activity execution summary.</param>
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecordSummary ActivityExecutionSummary)
    {
        /// Indicates whether the activity execution record has recorded retries.
        public bool HasRetries => ActivityExecutionSummary.Properties != null && ActivityExecutionSummary.Properties.TryGetValue("HasRetryAttempts", out var retryAttempts) && retryAttempts is JsonElement { ValueKind: JsonValueKind.True };
    }

    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }

    /// The activity to display executions for.
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// The activity execution record summaries.
    [Parameter] public ICollection<ActivityExecutionRecordSummary> ActivityExecutionSummaries { get; set; } = new List<ActivityExecutionRecordSummary>();

    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;

    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutionSummaries.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; }
    private PagedListResponse<RetryAttemptRecord> Retries { get; set; } = new();
    private DataPanelModel SelectedActivityState { get; set; } = new();
    private DataPanelModel SelectedOutcomesData { get; set; } = new();
    private DataPanelModel SelectedOutputData { get; set; } = new();
    private Timer? _refreshTimer;

    /// Refreshes the component.
    public async Task RefreshAsync()
    {
        await StopRefreshTimerAsync();
        SelectedItem = null;
        SelectedActivityState = new();
        SelectedOutcomesData = new();
        SelectedOutputData = new();
    }

    private void CreateSelectedItemDataModels(ActivityExecutionRecord? record)
    {
        if (record == null)
        {
            SelectedActivityState = new();
            SelectedOutcomesData = new();
            SelectedOutputData = new();
            return;
        }

        var activityState = record.ActivityState?
            .Where(x => !x.Key.StartsWith("_"))
            .Select(x => new DataPanelItem(x.Key, x.Value?.ToString()))
            .ToDataPanelModel();

        var outcomesData = record.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new DataPanelModel { new DataPanelItem("Outcomes", outcomesValue!.ToString()!) }
            : null;

        var outputData = new DataPanelModel();

        if (record.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData.Add(key, value?.ToString());

        SelectedActivityState = activityState ?? new();
        SelectedOutcomesData = outcomesData ?? new();
        SelectedOutputData = outputData;
    }

    private async Task RefreshSelectedItemAsync(string id)
    {
        SelectedItem = await ActivityExecutionService.GetAsync(id);
        var retryAttempts = await ActivityExecutionService.GetRetriesAsync(id);
        CreateSelectedItemDataModels(SelectedItem);
        Retries = retryAttempts;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        await StopRefreshTimerAsync();
        var id = arg.Item?.ActivityExecutionSummary.Id;

        if (id == null)
            return;

        await RefreshSelectedItemAsync(id);

        if (SelectedItem == null)
            return;

        if (SelectedItem.IsFused())
            return;

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