using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays the call stack for a selected activity execution.
/// </summary>
public partial class ActivityCallStack
{
    private readonly List<ActivityCallStackEntry> _entries = new();
    private bool _isLoading;
    private string? _lastKey;
    private bool _hasError;
    private int _selectedIndex = -1;
    private string? _selectedExecutionId;
    private bool _isPinned;

    /// <summary>
    /// The workflow instance ID associated with the selected activity.
    /// </summary>
    [Parameter] public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The selected activity from the designer.
    /// </summary>
    [Parameter] public JsonObject? SelectedActivity { get; set; }

    /// <summary>
    /// Indicates whether the call stack tab is currently active.
    /// </summary>
    [Parameter] public bool IsActive { get; set; }

    /// <summary>
    /// Gets a callback invoked when a call stack entry is selected.
    /// </summary>
    [Parameter] public Func<ActivityExecutionRecord, Task>? CallStackEntrySelected { get; set; }

    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;

    private IReadOnlyList<ActivityCallStackEntry> Entries => _entries;
    private bool IsLoading => _isLoading;
    private bool HasError => _hasError;
    private int SelectedIndex => _selectedIndex;
    private bool IsPinned => _isPinned;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var activityNodeId = SelectedActivity?.GetNodeId();
        var key = $"{WorkflowInstanceId}|{activityNodeId}|{IsActive}";

        if (key == _lastKey)
            return;

        _lastKey = key;

        if (!IsActive || string.IsNullOrWhiteSpace(activityNodeId) || string.IsNullOrWhiteSpace(WorkflowInstanceId))
        {
            _entries.Clear();
            _hasError = false;
            _selectedIndex = -1;
            _selectedExecutionId = null;
            return;
        }

        // When pinned, don't reload the call stack - just try to highlight the activity if it exists in the current stack
        if (_isPinned)
        {
            TrySelectActivityInCurrentStack(activityNodeId);
            return;
        }

        await LoadCallStackAsync(activityNodeId);
    }

    private async Task LoadCallStackAsync(string activityNodeId)
    {
        _isLoading = true;
        _hasError = false;
        _entries.Clear();
        _selectedIndex = -1;
        _selectedExecutionId = null;
        StateHasChanged();

        try
        {
            await ActivityRegistry.EnsureLoadedAsync();
            var summaries = (await ActivityExecutionService.ListSummariesAsync(WorkflowInstanceId!, activityNodeId)).ToList();

            if (!summaries.Any())
                return;

            var executionId = summaries.Last().Id;
            if (string.IsNullOrWhiteSpace(executionId))
                return;

            var callStack = await ActivityExecutionService.GetCallStackAsync(executionId, includeCrossWorkflowChain: true);

            foreach (var record in callStack.Items)
            {
                var activityDescriptor = ActivityRegistry.Find(record.ActivityType, record.ActivityTypeVersion);
                var activityDisplaySettings = ActivityDisplaySettingsRegistry.GetSettings(record.ActivityType);
                var duration = GetDuration(record);

                _entries.Add(new ActivityCallStackEntry(record, activityDescriptor, activityDisplaySettings, duration));
            }

            _selectedExecutionId = executionId;
            _selectedIndex = _entries.Count - 1;
        }
        catch
        {
            _hasError = true;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static TimeSpan? GetDuration(ActivityExecutionRecord record)
    {
        if (record.StartedAt == default)
            return null;

        var end = record.CompletedAt ?? DateTimeOffset.UtcNow;
        return end - record.StartedAt;
    }

    private async Task OnCallStackEntrySelected(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            _selectedIndex = -1;
            _selectedExecutionId = null;
            return;
        }

        var entry = _entries[index];
        _selectedIndex = index;
        _selectedExecutionId = entry.Record.Id;

        if (CallStackEntrySelected != null)
            await CallStackEntrySelected(entry.Record);
    }

    /// <summary>
    /// Attempts to find and select an activity in the current call stack by its node ID.
    /// This is used when the call stack is pinned to provide bidirectional highlighting.
    /// </summary>
    private void TrySelectActivityInCurrentStack(string activityNodeId)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Record.ActivityNodeId == activityNodeId)
            {
                _selectedIndex = i;
                _selectedExecutionId = _entries[i].Record.Id;
                StateHasChanged();
                return;
            }
        }

        // Activity not found in current call stack - don't change selection
    }
}
