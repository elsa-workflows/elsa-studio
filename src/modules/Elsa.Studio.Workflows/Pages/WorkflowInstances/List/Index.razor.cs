using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.List.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.List;

public partial class Index
{
    private MudTable<WorkflowInstanceRow> _table = null!;
    private HashSet<WorkflowInstanceRow> _selectedRows = new();
    private int _totalCount;

    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    
    private ICollection<WorkflowDefinitionSummary> WorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();
    private ICollection<WorkflowDefinitionSummary> SelectedWorkflowDefinitions { get; set; } = new List<WorkflowDefinitionSummary>();

    protected override async Task OnInitializedAsync()
    {
        await LoadWorkflowDefinitionsAsync();
    }

    private async Task LoadWorkflowDefinitionsAsync()
    {
        var workflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(new ListWorkflowDefinitionsRequest(), VersionOptions.Published);

        WorkflowDefinitions = workflowDefinitionsResponse.Items;
    }

    private async Task<TableData<WorkflowInstanceRow>> ServerReload(TableState state)
    {
        var request = new ListWorkflowInstancesRequest
        {
            Page = state.Page,
            PageSize = state.PageSize,
            DefinitionIds = SelectedWorkflowDefinitions.Select(x => x.DefinitionId).ToList(), 
        };

        var workflowInstancesResponse = await WorkflowInstanceService.ListAsync(request);
        var definitionVersionIds = workflowInstancesResponse.Items.Select(x => x.DefinitionVersionId).ToList();

        var workflowDefinitionVersionsResponse = await WorkflowDefinitionService.ListAsync(new ListWorkflowDefinitionsRequest
        {
            Ids = definitionVersionIds,
        });

        var workflowDefinitionVersionsLookup = workflowDefinitionVersionsResponse.Items.ToDictionary(x => x.Id);

        var rows = workflowInstancesResponse.Items.Select(x => new WorkflowInstanceRow(
            x.Id,
            x.CorrelationId,
            workflowDefinitionVersionsLookup[x.DefinitionVersionId],
            x.Version,
            x.Name,
            x.Status,
            x.SubStatus,
            x.CreatedAt,
            x.LastExecutedAt,
            x.FinishedAt,
            x.FaultedAt));

        _totalCount = (int)workflowInstancesResponse.TotalCount;
        return new TableData<WorkflowInstanceRow> { TotalItems = _totalCount, Items = rows };
    }
    
    private void View(string instanceId) => NavigationManager.NavigateTo($"/workflows/instances/{instanceId}/view");
    private void Reload() => _table.ReloadServerData();

    private Color GetSubStatusColor(WorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            WorkflowSubStatus.Suspended => Color.Warning,
            WorkflowSubStatus.Finished => Color.Success,
            WorkflowSubStatus.Faulted => Color.Error,
            WorkflowSubStatus.Cancelled => Color.Default,
            WorkflowSubStatus.Executing => Color.Primary,
            _ => Color.Default,
        };
    }

    private void OnViewClicked(string instanceId) => View(instanceId);
    private void OnRowClick(TableRowClickEventArgs<WorkflowInstanceRow> e) => View(e.Item.WorkflowInstanceId);

    private async Task OnDeleteClicked(WorkflowInstanceRow row)
    {
        var result = await DialogService.ShowMessageBox("Delete workflow instance?", "Are you sure you want to delete this workflow instance?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var instanceId = row.WorkflowInstanceId;
        await WorkflowInstanceService.DeleteAsync(instanceId);
        Reload();
    }
    
    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox("Delete selected workflow instances?", "Are you sure you want to delete the selected workflow instances?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        await WorkflowInstanceService.BulkDeleteAsync(workflowInstanceIds);
        Reload();
    }

    private async Task OnSelectedWorkflowDefinitionsChanged(IEnumerable<WorkflowDefinitionSummary> values)
    {
        SelectedWorkflowDefinitions = values.ToList();
        await _table.ReloadServerData();
    }
}