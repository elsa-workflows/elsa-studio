using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;

/// Represents a tab in the version history section of a workflow definition workspace.
public partial class VersionHistoryTab : IDisposable
{
    /// Gets or sets the definition ID.
    [Parameter]
    public string DefinitionId { get; set; } = default!;

    [CascadingParameter] private WorkflowDefinitionWorkspace Workspace { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    private HashSet<WorkflowDefinitionSummary> SelectedDefinitions { get; set; } = new();
    private MudTable<WorkflowDefinitionSummary> Table { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Workspace.WorkflowDefinitionUpdated += OnWorkflowDefinitionUpdated;
    }

    void IDisposable.Dispose()
    {
        Workspace.WorkflowDefinitionUpdated -= OnWorkflowDefinitionUpdated;
    }

    private async Task<TableData<WorkflowDefinitionSummary>> LoadVersionsAsync(TableState tableState)
    {
        var page = tableState.Page;
        var pageSize = tableState.PageSize;

        var request = new ListWorkflowDefinitionsRequest
        {
            DefinitionIds = new[] { DefinitionId },
            OrderDirection = OrderDirection.Descending,
            OrderBy = OrderByWorkflowDefinition.Version,
            Page = page,
            PageSize = pageSize
        };

        var response = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.ListAsync(request, VersionOptions.All));

        return new TableData<WorkflowDefinitionSummary>
        {
            Items = response.Items,
            TotalItems = (int)response.TotalCount
        };
    }

    private async Task ViewVersion(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        var workflowDefinition = (await WorkflowDefinitionService.FindByIdAsync(workflowDefinitionSummary.Id))!;
        Workspace.DisplayWorkflowDefinitionVersion(workflowDefinition);
    }

    private async Task ReloadTableAsync()
    {
        await Table.ReloadServerData();
    }

    private async Task OnWorkflowDefinitionUpdated() => await ReloadTableAsync();

    private async Task OnViewClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        await ViewVersion(workflowDefinitionSummary);
    }

    private async Task OnDeleteClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        var confirmed = await DialogService.ShowMessageBox("Delete version", "Are you sure you want to delete this version?");

        if (confirmed != true)
            return;

        await WorkflowDefinitionService.DeleteVersionAsync(workflowDefinitionSummary.Id);
        await ReloadTableAsync();
    }

    private async Task OnRowClick(TableRowClickEventArgs<WorkflowDefinitionSummary> arg)
    {
        await ViewVersion(arg.Item);
    }

    private async Task OnBulkDeleteClicked()
    {
        var confirmed = await DialogService.ShowMessageBox("Delete selected versions", "Are you sure you want to delete the selected versions?");

        if (confirmed != true)
            return;

        var ids = SelectedDefinitions.Select(x => x.Id).ToList();
        await WorkflowDefinitionService.BulkDeleteVersionsAsync(ids);
        await ReloadTableAsync();
    }

    private async Task OnRollbackClicked(WorkflowDefinitionSummary workflowDefinition)
    {
        var definitionId = workflowDefinition.DefinitionId;
        var version = workflowDefinition.Version;
        await WorkflowDefinitionService.RevertVersionAsync(definitionId, version);
        await Workspace.RefreshActiveWorkflowAsync();
        await ReloadTableAsync();
    }
}