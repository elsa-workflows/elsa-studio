using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;

/// Represents a tab in the version history section of a workflow definition workspace.
public partial class VersionHistoryTab : IDisposable
{
    /// Gets or sets the definition ID.
    [Parameter] public string DefinitionId { get; set; } = default!;

    /// <remarks>
    /// Null whenever no workspace of this exact type is cascaded - a host that renders this tab under its own
    /// copy of <see cref="WorkflowDefinitionWorkspace"/> leaves it unset. Every use below tolerates that: the
    /// version list stays readable and the actions that need a workspace turn themselves off, because throwing
    /// from <see cref="IDisposable.Dispose"/> here would tear down the entire Blazor circuit.
    /// </remarks>
    [CascadingParameter] private WorkflowDefinitionWorkspace? Workspace { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionHistoryService WorkflowDefinitionHistoryService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    
    private HashSet<WorkflowDefinitionSummary> SelectedDefinitions { get; set; } = new();
    private MudTable<WorkflowDefinitionSummary> Table { get; set; } = default!;
    private bool IsReadOnly => Workspace?.IsReadOnly ?? true;
    private bool HasWorkflowEditPermission => Workspace?.HasWorkflowEditPermission ?? false;
    private long _recordCount = 0;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Workspace is not null)
            Workspace.WorkflowDefinitionUpdated += OnWorkflowDefinitionUpdated;
    }

    void IDisposable.Dispose()
    {
        if (Workspace is not null)
            Workspace.WorkflowDefinitionUpdated -= OnWorkflowDefinitionUpdated;
    }

    private async Task<TableData<WorkflowDefinitionSummary>> LoadVersionsAsync(TableState tableState, CancellationToken cancellationToken)
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

        var response = await WorkflowDefinitionService.ListAsync(request, VersionOptions.All);

        _recordCount = response.TotalCount;
        return new TableData<WorkflowDefinitionSummary>
        {
            Items = response.Items,
            TotalItems = (int)response.TotalCount
        };
    }

    private async Task ViewVersionAsync(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        if (Workspace is null)
            return;

        var workflowDefinition = (await WorkflowDefinitionService.FindByIdAsync(workflowDefinitionSummary.Id))!;
        await Workspace.DisplayWorkflowDefinitionVersionAsync(workflowDefinition);
    }

    private async Task ReloadTableAsync()
    {
        await InvokeAsync(Table.ReloadServerData);
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
        await ViewVersionAsync(workflowDefinitionSummary);
    }

    private async Task OnDeleteClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(Localizer["Delete version {0}", workflowDefinitionSummary.Version], Localizer["Are you sure you want to delete this version?"]);

        if (confirmed != true)
            return;

        var workflowDefinitionVersion = WorkflowDefinitionVersion.FromDefinitionSummary(workflowDefinitionSummary);
        await WorkflowDefinitionService.DeleteVersionAsync(workflowDefinitionVersion);
        await ReloadTableAsync();

        if (Workspace?.IsSelectedDefinition(workflowDefinitionVersion.WorkflowDefinitionVersionId) == true)
            await Workspace.DisplayLatestWorkflowDefinitionVersionAsync();
    }

    private async Task OnRowClick(TableRowClickEventArgs<WorkflowDefinitionSummary> arg)
    {
        if (arg.Item is not null)
            await ViewVersionAsync(arg.Item);
    }

    private async Task OnBulkDeleteClicked()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(Localizer["Delete selected versions"], Localizer["Are you sure you want to delete the selected versions?"]);

        if (confirmed != true)
            return;

        var definitionVersions = SelectedDefinitions.Select(WorkflowDefinitionVersion.FromDefinitionSummary).ToList();
        await WorkflowDefinitionService.BulkDeleteVersionsAsync(definitionVersions);
        await ReloadTableAsync();

        if (Workspace is null)
            return;

        var selectedDefinition = Workspace.GetSelectedDefinition();
        if (selectedDefinition != null && definitionVersions.Any(x => x.WorkflowDefinitionVersionId == selectedDefinition.Id))
            await Workspace.DisplayLatestWorkflowDefinitionVersionAsync();
    }

    private async Task OnRollbackClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        if (Workspace is null)
            return;

        var definitionVersionId = workflowDefinitionSummary.Id;
        var definitionId = workflowDefinitionSummary.DefinitionId;
        var version = workflowDefinitionSummary.Version;
        var revertingVersion = new WorkflowDefinitionVersion(definitionId, definitionVersionId, version);
        var newDefinitionVersion = await WorkflowDefinitionHistoryService.RevertAsync(revertingVersion);
        await ReloadTableAsync();
        var newWorkflowDefinition = (await WorkflowDefinitionService.FindByIdAsync(newDefinitionVersion.Id))!;
        await Workspace.DisplayWorkflowDefinitionVersionAsync(newWorkflowDefinition);
        await Mediator.NotifyAsync(new WorkflowDefinitionReverted(newWorkflowDefinition));       
    }
}
