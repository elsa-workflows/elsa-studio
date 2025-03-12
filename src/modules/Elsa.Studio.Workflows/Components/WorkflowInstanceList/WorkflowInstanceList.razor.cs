using Elsa.Api.Client.Resources.Alterations.Contracts;
using Elsa.Api.Client.Resources.Alterations.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Components;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Refit;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Microsoft.Extensions.Logging;
using Elsa.Studio.Localization;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList;

/// Represents the workflow instances list page.
public partial class WorkflowInstanceList : IAsyncDisposable
{
    private MudTable<WorkflowInstanceRow> _table = null!;
    private HashSet<WorkflowInstanceRow> _selectedRows = new();
    private int _totalCount;
    private Timer? _elapsedTimer;

    /// <summary>
    /// An event that is invoked when a workflow definition is edited.
    /// </summary>
    [Parameter] public EventCallback<string> ViewWorkflowInstance { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private ILogger<WorkflowInstanceList> Logger { get; set; } = default!;

    private ICollection<WorkflowDefinitionSummary> WorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();
    private ICollection<WorkflowDefinitionSummary> SelectedWorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();

    private string SearchTerm { get; set; } = string.Empty;
    private bool? HasIncidents { get; set; }
    private bool IsDateRangePopoverOpen { get; set; }

    /// The selected statuses to filter by.
    private ICollection<WorkflowStatus> SelectedStatuses { get; set; } = new List<WorkflowStatus>();

    /// The selected sub-statuses to filter by.
    private ICollection<WorkflowSubStatus> SelectedSubStatuses { get; set; } = new List<WorkflowSubStatus>();

    // The selected timestamp filters to filter by.
    private ICollection<TimestampFilterModel> TimestampFilters { get; set; } = new List<TimestampFilterModel>();


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadWorkflowDefinitionsAsync();

        // Disable auto refresh until we implement a way to maintain the selected state, pagination etc. 
        //StartElapsedTimer();
    }

    private async Task LoadWorkflowDefinitionsAsync()
    {
        var workflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(new(), VersionOptions.LatestOrPublished);
        var workflowDefinitions = workflowDefinitionsResponse.Items;

        // Filter the definitions to ensure only the one with the highest version for each DefinitionId remains
        var filteredWorkflowDefinitions = workflowDefinitions
            .GroupBy(definition => definition.DefinitionId) // Group by DefinitionId
            .Select(group => group.OrderByDescending(definition => definition.Version).First()) // Get the highest version in each group
            .ToList();

        WorkflowDefinitions = filteredWorkflowDefinitions;
    }

    private async Task<TableData<WorkflowInstanceRow>> LoadData(TableState state, CancellationToken cancellationToken)
    {
        var request = new ListWorkflowInstancesRequest
        {
            Page = state.Page,
            PageSize = state.PageSize,
            DefinitionIds = SelectedWorkflowDefinitions.Select(x => x.DefinitionId).ToList(),
            Statuses = SelectedStatuses,
            SubStatuses = SelectedSubStatuses,
            SearchTerm = SearchTerm,
            HasIncidents = HasIncidents,
            IsSystem = false,
            OrderBy = GetOrderBy(state.SortLabel),
            OrderDirection = state.SortDirection == SortDirection.Descending ? OrderDirection.Descending : OrderDirection.Ascending,
            TimestampFilters = TimestampFilters.Select(Map).Where(x => x.Timestamp.Date > DateTime.MinValue && !string.IsNullOrWhiteSpace(x.Column)).ToList()
        };

        try
        {
            var workflowInstancesResponse = await WorkflowInstanceService.ListAsync(request, cancellationToken);
            var definitionVersionIds = workflowInstancesResponse.Items.Select(x => x.DefinitionVersionId).Distinct().ToList();

            var workflowDefinitionVersionsResponse = await WorkflowDefinitionService.ListAsync(new()
            {
                Ids = definitionVersionIds,
            }, cancellationToken: cancellationToken);

            var workflowDefinitionVersionsLookup = workflowDefinitionVersionsResponse.Items.ToDictionary(x => x.Id);

            // Select any workflow instances for which no corresponding workflow definition version was found.
            // This can happen when a workflow definition is deleted.
            var missingWorkflowDefinitionVersionIds = definitionVersionIds.Except(workflowDefinitionVersionsLookup.Keys).ToList();
            var filteredWorkflowInstances = workflowInstancesResponse.Items.Where(x => !missingWorkflowDefinitionVersionIds.Contains(x.DefinitionVersionId));

            var rows = filteredWorkflowInstances.Select(x => new WorkflowInstanceRow(
                x.Id,
                x.CorrelationId,
                workflowDefinitionVersionsLookup[x.DefinitionVersionId],
                x.Version,
                x.Name,
                x.Status,
                x.SubStatus,
                x.IncidentCount,
                x.CreatedAt,
                x.UpdatedAt,
                x.FinishedAt));

            _totalCount = (int)workflowInstancesResponse.TotalCount;
            return new() { TotalItems = _totalCount, Items = rows };
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a cancellation.");
            _totalCount = 0;
            return new() { TotalItems = 0, Items = Array.Empty<WorkflowInstanceRow>() };
        }
        catch (ApiException ex) when (ex.InnerException is TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a cancellation.");
            _totalCount = 0;
            return new() { TotalItems = 0, Items = Array.Empty<WorkflowInstanceRow>() };
        }

    }

    private async Task<PagedListResponse<WorkflowInstanceSummary>> TryListWorkflowDefinitionsAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await WorkflowInstanceService.ListAsync(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.InnerException is TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a timeout.");
            return new()
            {
                Items = []
            };
        }
    }

    private TimestampFilter Map(TimestampFilterModel source)
    {
        var date = !string.IsNullOrWhiteSpace(source.Date) ? DateTime.Parse(source.Date) : DateTime.MinValue;
        var time = !string.IsNullOrWhiteSpace(source.Time) ? TimeSpan.Parse(source.Time) : TimeSpan.Zero;
        var dateTime = date.Add(time);
        var timestamp = dateTime == DateTime.MinValue ? DateTimeOffset.MinValue : new(dateTime);

        return new()
        {
            Column = source.Column,
            Operator = source.Operator,
            Timestamp = timestamp
        };
    }

    private OrderByWorkflowInstance? GetOrderBy(string sortLabel)
    {
        return sortLabel switch
        {
            "Name" => OrderByWorkflowInstance.Name,
            "Finished" => OrderByWorkflowInstance.Finished,
            "Created" => OrderByWorkflowInstance.Created,
            "LastExecuted" => OrderByWorkflowInstance.LastExecuted,
            _ => null
        };
    }

    private async Task ViewAsync(string instanceId)
    {
        await ViewWorkflowInstance.InvokeAsync(instanceId);
    }

    private void Reload() => _table.ReloadServerData();

    private bool FilterWorkflowDefinitions(WorkflowDefinitionSummary workflowDefinition, string term)
    {
        var trimmedTerm = term.Trim();

        if (string.IsNullOrEmpty(term))
            return true;

        var sources = new[]
        {
            (string?)workflowDefinition.Name
        };

        return sources.Any(x => x?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) == true);
    }

    private Color GetSubStatusColor(WorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            WorkflowSubStatus.Pending => Color.Tertiary,
            WorkflowSubStatus.Suspended => Color.Warning,
            WorkflowSubStatus.Finished => Color.Success,
            WorkflowSubStatus.Faulted => Color.Error,
            WorkflowSubStatus.Cancelled => Color.Tertiary,
            WorkflowSubStatus.Executing => Color.Primary,
            _ => Color.Default,
        };
    }

    private string? GetWorkflowDefinitionDisplayText(WorkflowDefinitionSummary? definition)
    {
        return definition?.Name;
    }

    private void ToggleDateRangePopover()
    {
        IsDateRangePopoverOpen = !IsDateRangePopoverOpen;
    }

    private void OnViewClicked(string instanceId) => _ = ViewAsync(instanceId);
    private void OnRowClick(TableRowClickEventArgs<WorkflowInstanceRow> e) => _ = ViewAsync(e.Item!.WorkflowInstanceId);

    private async Task OnDeleteClicked(WorkflowInstanceRow row)
    {
        var result = await DialogService.ShowMessageBox(Localizer["Delete workflow instance?"], Localizer["Are you sure you want to delete this workflow instance?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var instanceId = row.WorkflowInstanceId;
        await WorkflowInstanceService.DeleteAsync(instanceId);
        Reload();
    }

    private async Task OnCancelClicked(WorkflowInstanceRow row)
    {
        var result = await DialogService.ShowMessageBox(Localizer["Cancel workflow instance?"], Localizer["Are you sure you want to cancel this workflow instance?"], yesText: Localizer["Yes"], cancelText: Localizer["No"]);

        if (result != true)
            return;

        var instanceId = row.WorkflowInstanceId;
        await WorkflowInstanceService.CancelAsync(instanceId);
        Reload();
    }

    private async Task OnDownloadClicked(WorkflowInstanceRow workflowInstanceRow)
    {
        var download = await WorkflowInstanceService.ExportAsync(workflowInstanceRow.WorkflowInstanceId);
        var fileName = $"{workflowInstanceRow.Name?.Kebaberize() ?? workflowInstanceRow.WorkflowInstanceId}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox(Localizer["Delete selected workflow instances?"], Localizer["Are you sure you want to delete the selected workflow instances?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        await WorkflowInstanceService.BulkDeleteAsync(workflowInstanceIds);
        Reload();
    }

    private async Task OnBulkCancelClicked()
    {
        var reference = await DialogService.ShowAsync<BulkCancelDialog>(Localizer["Cancel selected workflow instances?"]);
        var dialogResult = await reference.Result;
        
        if (dialogResult == null || dialogResult.Canceled)
            return;

        var applyToAllMatches = (bool)(dialogResult.Data ?? false);

        if (applyToAllMatches)
        {
            var plan = new AlterationPlanParams
            {
                Alterations = [new Cancel()],
                Filter = new()
                {
                    EmptyFilterSelectsAll = true,
                    HasIncidents = HasIncidents,
                    IsSystem = false,
                    SearchTerm = SearchTerm,
                    Statuses = SelectedStatuses.Any() ? SelectedStatuses : null,
                    SubStatuses = SelectedSubStatuses.Any() ? SelectedSubStatuses : null,
                    TimestampFilters = TimestampFilters.Any() ? TimestampFilters.Select(Map).Where(x => x.Timestamp.Date > DateTime.MinValue && !string.IsNullOrWhiteSpace(x.Column)).ToList() : null,
                    DefinitionIds = SelectedWorkflowDefinitions.Any() ? SelectedWorkflowDefinitions.Select(x => x.DefinitionId).ToList() : null
                }
            };

            var alterationsApi = await BackendApiClientProvider.GetApiAsync<IAlterationsApi>();
            await alterationsApi.Submit(plan);
            Snackbar.Add("Workflow instances are being cancelled.", Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }
        else
        {
            var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
            var request = new BulkCancelWorkflowInstancesRequest
            {
                Ids = workflowInstanceIds
            };
            await WorkflowInstanceService.BulkCancelAsync(request);
        }

        Reload();
    }

    private Task OnImportClicked()
    {
        return DomAccessor.ClickElementAsync("#instance-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        var maxAllowedSize = 1024 * 1024 * 10; // 10 MB
        var streamParts = files.Select(x => new StreamPart(x.OpenReadStream(maxAllowedSize), x.Name, x.ContentType)).ToList();
        var count = await WorkflowInstanceService.BulkImportAsync(streamParts);
        var message = count == 1 ? Localizer["Successfully imported one instance"] : Localizer["Successfully imported {0} instances", count];
        Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }

    private async Task OnBulkExportClicked()
    {
        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        var download = await WorkflowInstanceService.BulkExportAsync(workflowInstanceIds);
        var fileName = download.FileName;
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnSelectedWorkflowDefinitionsChanged(IEnumerable<WorkflowDefinitionSummary> values)
    {
        SelectedWorkflowDefinitions = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSelectedStatusesChanged(IEnumerable<WorkflowStatus> values)
    {
        SelectedStatuses = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSelectedSubStatusesChanged(IEnumerable<WorkflowSubStatus> values)
    {
        SelectedSubStatuses = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSearchTermChanged(string text)
    {
        if (text != SearchTerm)
        {
            SearchTerm = text;
            await _table.ReloadServerData();
        }
    }

    private async Task OnHasIncidentsChanged(bool? value)
    {
        HasIncidents = value;
        await _table.ReloadServerData();
    }

    private void OnAddTimestampFilterClicked()
    {
        TimestampFilters.Add(new());
        StateHasChanged();
    }

    private void OnRemoveTimestampFilterClicked(TimestampFilterModel filter)
    {
        TimestampFilters.Remove(filter);
        StateHasChanged();
    }

    private void OnClearTimestampFiltersClicked()
    {
        TimestampFilters.Clear();
        StateHasChanged();
    }

    private async Task OnApplyTimestampFiltersClicked()
    {
        await _table.ReloadServerData();
        ToggleDateRangePopover();
    }

    private void StartElapsedTimer()
    {
        _elapsedTimer ??= new(_ => InvokeAsync(async () => await _table.ReloadServerData()), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void StopElapsedTimer()
    {
        if (_elapsedTimer != null)
        {
            _elapsedTimer.Dispose();
            _elapsedTimer = null;
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        StopElapsedTimer();
        return ValueTask.CompletedTask;
    }
}
