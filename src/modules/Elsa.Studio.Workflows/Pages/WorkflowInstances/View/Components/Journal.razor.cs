using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

/// <summary>
/// Displays the journal for a workflow instance.
/// </summary>
public partial class Journal : IAsyncDisposable
{
    private MudTimeline _timeline = default!;
    private IList<JournalEntry> _currentEntries = default!;
    private HubConnection? _hubConnection;
    private WorkflowInstance? _workflowInstance;

    /// <summary>
    /// Gets or sets a callback that is invoked when a journal entry is selected.
    /// </summary>
    [Parameter]
    public Func<JournalEntry, Task>? JournalEntrySelected { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;

    private WorkflowInstance? WorkflowInstance { get; set; }
    private IWorkflowInstanceObserver WorkflowInstanceObserver { get; set; } = default!;
    private TimeMetricMode TimeMetricMode { get; set; } = TimeMetricMode.Relative;
    private bool ShowScopedEvents { get; set; } = true;
    private bool ShowIncidents { get; set; }
    private JournalEntry? SelectedEntry { get; set; }
    private JournalFilter? JournalFilter { get; set; }
    private Virtualize<JournalEntry> VirtualizeComponent { get; set; } = default!;
    private int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// Sets the workflow instance to display the journal for.
    /// </summary>
    public async Task SetWorkflowInstanceAsync(WorkflowInstance workflowInstance, JournalFilter? filter = default)
    {
        WorkflowInstance = workflowInstance;
        JournalFilter = filter;
        await EnsureActivityDescriptorsAsync();
        await RefreshJournalAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedEntry = null;
        SelectedIndex = -1;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await EnsureActivityDescriptorsAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance;

            // If the workflow instance is still running, observe it.
            if (WorkflowInstance?.Status == WorkflowStatus.Running)
                await ObserveWorkflowInstanceAsync();
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            UpdateJournalHack();
        }
    }

    private async Task EnsureActivityDescriptorsAsync() => await ActivityRegistry.EnsureLoadedAsync();

    private async Task RefreshJournalAsync()
    {
        if (VirtualizeComponent != null!)
            await VirtualizeComponent.RefreshDataAsync();
    }

    private TimeSpan GetTimeMetric(WorkflowExecutionLogRecord current, WorkflowExecutionLogRecord? previous)
    {
        return TimeMetricMode switch
        {
            TimeMetricMode.Relative => previous == null ? TimeSpan.Zero : current.Timestamp - previous.Timestamp,
            TimeMetricMode.Accumulated => SumExecutionTime(current),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private TimeSpan SumExecutionTime(WorkflowExecutionLogRecord current) => current.Timestamp - WorkflowInstance!.CreatedAt;

    private async ValueTask<ItemsProviderResult<JournalEntry>> FetchExecutionLogRecordsAsync(ItemsProviderRequest request)
    {
        if (WorkflowInstance == null)
            return new ItemsProviderResult<JournalEntry>(Enumerable.Empty<JournalEntry>(), 0);

        await EnsureActivityDescriptorsAsync();

        var take = request.Count == 0 ? 10 : request.Count;
        var skip = request.StartIndex > 0 ? request.StartIndex - 1 : 0;
        var filter = new JournalFilter();

        if (ShowScopedEvents)
            filter.ActivityIds = JournalFilter?.ActivityIds;

        if (ShowIncidents)
            filter.EventNames = new[] { "Faulted" };

        var response = await WorkflowInstanceService.GetJournalAsync(WorkflowInstance.Id, filter, skip, take);
        var totalCount = request.StartIndex > 0 ? response.TotalCount - 1 : response.TotalCount;
        var records = response.Items.ToArray();
        var localSkip = request.StartIndex > 0 ? 1 : 0;
        var entries = records.Skip(localSkip).Select((record, index) =>
        {
            var previousIndex = index - 1;
            var previousRecord = previousIndex >= 0 ? records[previousIndex] : default;
            var activityDescriptor = ActivityRegistry.Find(record.ActivityType, record.ActivityTypeVersion);
            var activityDisplaySettings = ActivityDisplaySettingsRegistry.GetSettings(record.ActivityType);
            var isEven = index % 2 == 0;
            var timeMetric = GetTimeMetric(record, previousRecord);

            return new JournalEntry(
                record,
                activityDescriptor,
                activityDisplaySettings,
                isEven,
                timeMetric);
        }).ToList();

        _currentEntries = entries;

        return new ItemsProviderResult<JournalEntry>(entries, (int)totalCount);
    }

    private void UpdateJournalHack()
    {
        // A little hack to ensure the journal is refreshed.
        // Sometimes the journal doesn't update on first load, until a UI refresh is triggered.
        // We do it a few times, first quickly, but if that was too soon, try it again a few times, but slower.
        foreach (var timeout in new[] { 10, 100, 500, 1000 })
            _ = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, timeout, Timeout.Infinite);
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(WorkflowInstance!.Id);
        WorkflowInstanceObserver.WorkflowJournalUpdated += async _ => await InvokeAsync(async () =>
        {
            await RefreshJournalAsync();
            UpdateJournalHack();
        });
    }

    private async Task OnTimeMetricButtonToggleChanged(bool value)
    {
        TimeMetricMode = value ? TimeMetricMode.Accumulated : TimeMetricMode.Relative;
        await RefreshJournalAsync();
    }

    private async Task OnScopeToggleChanged(bool value)
    {
        ShowScopedEvents = value;
        await RefreshJournalAsync();
    }

    private async Task OnShowIncidentsToggleChanged(bool value)
    {
        ShowIncidents = value;
        await RefreshJournalAsync();
    }

    private async Task OnJournalEntrySelected(int index)
    {
        if (index < 0 || index >= _currentEntries.Count)
        {
            SelectedEntry = null;
            SelectedIndex = -1;
        }

        var entry = index >= 0 && index < _currentEntries.Count ? _currentEntries[index] : null;
        SelectedEntry = entry;
        SelectedIndex = index;

        if (JournalEntrySelected != null && entry != null)
            await JournalEntrySelected(entry);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }
}