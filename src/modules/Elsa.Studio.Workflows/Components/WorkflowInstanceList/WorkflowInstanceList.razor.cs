using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Alterations.Contracts;
using Elsa.Api.Client.Resources.Alterations.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Api.Client.Shared.Enums;
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

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList;

/// Represents the workflow instances list page.
public partial class WorkflowInstanceList : IAsyncDisposable
{
    private MudTable<WorkflowInstanceRow> _table = null!;
    private HashSet<WorkflowInstanceRow> _selectedRows = new();
    private int _totalCount;
    private Timer? _elapsedTimer;
    private bool _initializedFromQuery;

    /// <summary>
    /// An event that is invoked when a workflow definition is edited.
    /// </summary>
    [Parameter] public EventCallback<string> ViewWorkflowInstance { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IUserMessageService UserMessageService { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private ILogger<WorkflowInstanceList> Logger { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ICollection<WorkflowDefinitionSummary> WorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();
    private ICollection<WorkflowDefinitionSummary> SelectedWorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();

    /// The selected statuses to filter by.
    private ICollection<WorkflowStatus> SelectedStatuses { get; set; } = new List<WorkflowStatus>();

    /// The selected sub-statuses to filter by.
    private ICollection<WorkflowSubStatus> SelectedSubStatuses { get; set; } = new List<WorkflowSubStatus>();

    // The selected timestamp filters to filter by.
    private ICollection<TimestampFilterModel> TimestampFilters { get; set; } = new List<TimestampFilterModel>();

    private string SearchTerm { get; set; } = string.Empty;
    private bool IsPolling { get; set; } = true;
    private bool? HasIncidents { get; set; }
    private bool IsDateRangePopoverOpen { get; set; }

    private void Reload() => _table.ReloadServerData();
    private async Task ViewAsync(string instanceId) => await ViewWorkflowInstance.InvokeAsync(instanceId);
    private string? GetWorkflowDefinitionDisplayText(WorkflowDefinitionSummary? definition) => definition?.Name;
    private void ToggleDateRangePopover() => IsDateRangePopoverOpen = !IsDateRangePopoverOpen;
    private void OnViewClicked(string instanceId) => _ = ViewAsync(instanceId);
    private void OnRowClick(TableRowClickEventArgs<WorkflowInstanceRow> e) => _ = ViewAsync(e.Item!.WorkflowInstanceId);
    private Task OnImportClicked() => DomAccessor.ClickElementAsync("#instance-file-upload-button-wrapper input[type=file]");


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        // Load workflow definitions first so query parsing can match definitions
        await LoadWorkflowDefinitionsAsync();

        // Try to read filters from query string (must happen after workflow definitions are loaded so SelectedWorkflowDefinitions can be resolved)
        ParseQueryParameters();
        StartElapsedTimer();
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

    private void ParseQueryParameters()
    {
        if (_initializedFromQuery)
            return;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = ParseQueryString(uri.Query);

        // Search term
        if (query.TryGetValue("search", out var searchValues))
            SearchTerm = searchValues ?? string.Empty;

        // Has incidents
        if (query.TryGetValue("hasIncidents", out var incidentsValues) && bool.TryParse(incidentsValues, out var incidents))
            HasIncidents = incidents;

        // Statuses (comma separated names)
        if (query.TryGetValue("statuses", out var statusesValues) && !string.IsNullOrWhiteSpace(statusesValues))
        {
            SelectedStatuses = statusesValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s =>
                {
                    return Enum.TryParse<WorkflowStatus>(s, true, out var st) ? (WorkflowStatus?)st : null;
                })
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }

        // SubStatuses
        if (query.TryGetValue("substatuses", out var substatusesValues) && !string.IsNullOrWhiteSpace(substatusesValues))
        {
            SelectedSubStatuses = substatusesValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s =>
                {
                    return Enum.TryParse<WorkflowSubStatus>(s, true, out var st) ? (WorkflowSubStatus?)st : null;
                })
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }

        // Definitions (comma separated definition ids)
        if (query.TryGetValue("defs", out var defsValues) && !string.IsNullOrWhiteSpace(defsValues))
        {
            var ids = defsValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
            SelectedWorkflowDefinitions = WorkflowDefinitions.Where(wd => ids.Contains(wd.DefinitionId)).ToList();
        }

        // Timestamp filters - encoded as ts=Column|Operator|Date|Time (multiple separated by comma)
        if (query.TryGetValue("ts", out var tsValues) && !string.IsNullOrWhiteSpace(tsValues))
        {
            TimestampFilters = tsValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.Split('|'))
                .Where(parts => parts.Length >= 3)
                .Select(parts =>
                {
                    var model = new TimestampFilterModel
                    {
                        Column = parts[0],
                        Date = parts[2]
                    };

                    if (Enum.TryParse<TimestampFilterOperator>(parts[1], true, out var op))
                        model.Operator = op;

                    if (parts.Length >= 4)
                        model.Time = parts[3];

                    return model;
                })
                .ToList();
        }
        
        _initializedFromQuery = true;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query)) return result;
        if (query.StartsWith("?")) query = query[1..];

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx >= 0)
            {
                var key = Uri.UnescapeDataString(part[..idx]);
                var val = Uri.UnescapeDataString(part[(idx + 1)..]);
                result[key] = val;
            }
            else
            {
                var key = Uri.UnescapeDataString(part);
                result[key] = string.Empty;
            }
        }
        return result;
    }

    private async Task<TableData<WorkflowInstanceRow>> LoadData(TableState state, CancellationToken cancellationToken)
    {
        // Ensure query parameters are parsed before the first load (in case they weren't parsed in OnInitialized)
        ParseQueryParameters();

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
                workflowDefinitionVersionsLookup[x.DefinitionVersionId]?.Name ?? string.Empty,
                x.Version,
                x.Name,
                x.Status,
                x.SubStatus,
                x.IncidentCount,
                x.CreatedAt,
                x.UpdatedAt,
                x.FinishedAt));

            _totalCount = (int)workflowInstancesResponse.TotalCount;

            // Update URL to reflect current table state & filters
            TryUpdateUrlFromState(state);

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

    private void TryUpdateUrlFromState(TableState state)
    {
        try
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var baseUri = uri.GetLeftPart(UriPartial.Path);

            var query = new Dictionary<string, string?>();

            // paging & sorting
            query["page"] = state.Page.ToString();
            query["pageSize"] = state.PageSize.ToString();
            if (!string.IsNullOrWhiteSpace(state.SortLabel))
                query["sort"] = state.SortLabel;
            query["sortDir"] = state.SortDirection == SortDirection.Descending ? "desc" : "asc";

            // filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                query["search"] = SearchTerm;

            if (HasIncidents != null)
                query["hasIncidents"] = HasIncidents.ToString();

            if (SelectedStatuses.Any())
                query["statuses"] = string.Join(',', SelectedStatuses.Select(s => s.ToString()));

            if (SelectedSubStatuses.Any())
                query["substatuses"] = string.Join(',', SelectedSubStatuses.Select(s => s.ToString()));

            if (SelectedWorkflowDefinitions.Any())
                query["defs"] = string.Join(',', SelectedWorkflowDefinitions.Select(d => d.DefinitionId));

            if (TimestampFilters.Any())
            {
                // encode as Column|Operator|Date|Time per filter and join with comma
                var encoded = TimestampFilters.Select(tf => string.Join('|', new[] { tf.Column ?? string.Empty, tf.Operator.ToString(), tf.Date ?? string.Empty, tf.Time ?? string.Empty }));
                query["ts"] = string.Join(',', encoded);
            }
            
            // Build query string manually to avoid dependency on WebUtilities in all targets
            var kv = query.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();
            if (kv.Any())
            {
                var qs = string.Join('&', kv.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value!)));
                var newUri = baseUri + "?" + qs;

                // Only navigate if URL actually changed (prevents unnecessary history entries / re-render loops)
                if (!string.Equals(NavigationManager.Uri, newUri, StringComparison.Ordinal))
                    NavigationManager.NavigateTo(newUri, replace: true);
            }
            else
            {
                if (!string.Equals(NavigationManager.Uri, baseUri, StringComparison.Ordinal))
                    NavigationManager.NavigateTo(baseUri, replace: true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update URL with table state.");
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
                Items = Array.Empty<WorkflowInstanceSummary>()
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
        _selectedRows.Clear();
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
            var cancel = new JsonObject
            {
                ["type"] = "Cancel"
            };

            var plan = new AlterationPlanParams
            {
                Alterations = [cancel],
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
            UserMessageService.ShowSnackbarTextMessage("Workflow instances are being cancelled.", Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
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

    private async Task OnBulkExportClicked()
    {
        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        var download = await WorkflowInstanceService.BulkExportAsync(workflowInstanceIds);
        var fileName = download.FileName;
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        var maxAllowedSize = 1024 * 1024 * 10; // 10 MB
        var streamParts = files.Select(x => new StreamPart(x.OpenReadStream(maxAllowedSize), x.Name, x.ContentType)).ToList();
        var count = await WorkflowInstanceService.BulkImportAsync(streamParts);
        var message = count == 1 ? Localizer["Successfully imported one instance"] : Localizer["Successfully imported {0} instances", count];
        UserMessageService.ShowSnackbarTextMessage(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
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

    private void OnPollingChanged(bool enabled)
    {
        IsPolling = enabled;

        if (IsPolling)
            StartElapsedTimer();
        else
            StopElapsedTimer();
    }

    private void StartElapsedTimer() => _elapsedTimer ??= new(_ => InvokeAsync(async () => await _table.ReloadServerData()), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

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