using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;

/// Represents a tab in the version history section of a workflow definition workspace.
public partial class VersionHistoryTab : IDisposable
{
    /// Gets or sets the definition ID.
    [Parameter] public string DefinitionId { get; set; } = default!;

    /// Gets or sets a callback invoked when the workflow definition is about to be reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionReverting { get; set; }

    /// Gets or sets a callback invoked when the workflow definition is reverted to an earlier version.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionReverted { get; set; }

    /// Gets or sets a callback invoked when the workflow definition version is about to be deleted.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionDeleting { get; set; }

    /// Gets or sets a callback invoked when the workflow definition version is about to be deleted.
    [Parameter] public EventCallback<WorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionDeleted { get; set; }

    /// Gets or sets a callback invoked when workflow definition versions are about to be deleted in bulk.
    [Parameter] public EventCallback<BulkWorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionBulkDeleting { get; set; }

    /// Gets or sets a callback invoked when workflow definition versions have been deleted in bulk.
    [Parameter] public EventCallback<BulkWorkflowDefinitionVersionEventArgs> WorkflowDefinitionVersionBulkDeleted { get; set; }

    [CascadingParameter] private WorkflowDefinitionWorkspace Workspace { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    private HashSet<WorkflowDefinitionSummary> SelectedDefinitions { get; set; } = new();
    private MudTable<WorkflowDefinitionSummary> Table { get; set; } = default!;
    private bool IsReadOnly => Workspace.IsReadOnly;
    private bool HasWorkflowEditPermission => Workspace.HasWorkflowEditPermission;
    private long _recordCount = 0;

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
            DefinitionIds = [DefinitionId],
            OrderDirection = OrderDirection.Descending,
            OrderBy = OrderByWorkflowDefinition.Version,
            Page = page,
            PageSize = pageSize
        };

        var response = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.ListAsync(request, VersionOptions.All));

        _recordCount = response.TotalCount;
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

    private bool CanRollback(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        return HasWorkflowEditPermission && workflowDefinitionSummary is { IsLatest: false };
    }

    private bool CanDelete(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        return HasWorkflowEditPermission && _recordCount > 1;
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

        var definitionVersion = new WorkflowDefinitionVersion(workflowDefinitionSummary.DefinitionId, workflowDefinitionSummary.Id, workflowDefinitionSummary.Version);
        var eventArgs = new WorkflowDefinitionVersionEventArgs(definitionVersion);
        if (WorkflowDefinitionVersionDeleting.HasDelegate) await WorkflowDefinitionVersionDeleting.InvokeAsync(eventArgs);
        await WorkflowDefinitionService.DeleteVersionAsync(workflowDefinitionSummary.Id);
        if (WorkflowDefinitionVersionDeleting.HasDelegate) await WorkflowDefinitionVersionDeleted.InvokeAsync(eventArgs);
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

        var definitionVersions = SelectedDefinitions.Select(WorkflowDefinitionVersion.FromDefinitionSummary).ToList();

        if (WorkflowDefinitionVersionBulkDeleting.HasDelegate)
            await WorkflowDefinitionVersionBulkDeleting.InvokeAsync(new BulkWorkflowDefinitionVersionEventArgs(definitionVersions));

        if (WorkflowDefinitionVersionDeleting.HasDelegate)
        {
            foreach (var definition in SelectedDefinitions)
            {
                var definitionVersion = new WorkflowDefinitionVersion(definition.DefinitionId, definition.Id, definition.Version);
                var eventArgs = new WorkflowDefinitionVersionEventArgs(definitionVersion);
                await WorkflowDefinitionVersionDeleting.InvokeAsync(eventArgs);
            }
        }

        var ids = SelectedDefinitions.Select(x => x.Id).ToList();
        await WorkflowDefinitionService.BulkDeleteVersionsAsync(ids);

        if (WorkflowDefinitionVersionDeleting.HasDelegate)
        {
            foreach (var definition in SelectedDefinitions)
            {
                var definitionVersion = new WorkflowDefinitionVersion(definition.DefinitionId, definition.Id, definition.Version);
                var eventArgs = new WorkflowDefinitionVersionEventArgs(definitionVersion);
                await WorkflowDefinitionVersionDeleted.InvokeAsync(eventArgs);
            }
        }

        if (WorkflowDefinitionVersionBulkDeleted.HasDelegate)
            await WorkflowDefinitionVersionBulkDeleted.InvokeAsync(new BulkWorkflowDefinitionVersionEventArgs(definitionVersions));
        await ReloadTableAsync();
    }

    private async Task OnRollbackClicked(WorkflowDefinitionSummary workflowDefinition)
    {
        var definitionVersionId = workflowDefinition.Id;
        var definitionId = workflowDefinition.DefinitionId;
        var version = workflowDefinition.Version;
        var revertingVersion = new WorkflowDefinitionVersion(definitionVersionId, definitionId, version);
        var revertingEventArgs = new WorkflowDefinitionVersionEventArgs(revertingVersion);
        if (WorkflowDefinitionReverting.HasDelegate) await WorkflowDefinitionReverting.InvokeAsync(revertingEventArgs);
        var newDefinition = await WorkflowDefinitionService.RevertVersionAsync(definitionId, version);
        var revertedVersion = new WorkflowDefinitionVersion(definitionId, newDefinition.Id, newDefinition.Version);
        var revertedEventArgs = new WorkflowDefinitionVersionEventArgs(revertedVersion);
        if (WorkflowDefinitionReverting.HasDelegate) await WorkflowDefinitionReverted.InvokeAsync(revertedEventArgs);
        await Workspace.RefreshActiveWorkflowAsync();
        await ReloadTableAsync();
    }
}