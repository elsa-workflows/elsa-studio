using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.VersionHistory;

public partial class VersionHistoryTab : IDisposable
{
    [Parameter] public string DefinitionId { get; set; } = default!;
    [CascadingParameter] public Workspace Workspace { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    private HashSet<WorkflowDefinitionSummary> SelectedDefinitions { get; set; } = new();
    private MudTable<WorkflowDefinitionSummary> Table { get; set; } = default!;

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

        var response = await WorkflowDefinitionService.ListAsync(request, VersionOptions.All);

        return new TableData<WorkflowDefinitionSummary>
        {
            Items = response.Items,
            TotalItems = response.TotalCount
        };
    }
    
    private async Task ViewVersion(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        var workflowDefinition = (await WorkflowDefinitionService.FindByIdAsync(workflowDefinitionSummary.Id))!;

        if (workflowDefinition.IsLatest)
        {
            Workspace.ResumeEditing();
        }
        else
        {
            Workspace.DisplayWorkflowDefinitionVersion(workflowDefinition);
        }
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        await Table.ReloadServerData();
    }

    private async Task OnViewClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        await ViewVersion(workflowDefinitionSummary);
    }

    private Task OnDeleteClicked(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        return Task.CompletedTask;
    }

    private async Task OnRowClick(TableRowClickEventArgs<WorkflowDefinitionSummary> arg)
    {
        await ViewVersion(arg.Item);
    }

    private Task OnBulkDeleteClicked()
    {
        return Task.CompletedTask;
    }

    private Task OnRollbackClicked(WorkflowDefinitionSummary context)
    {
        return Task.CompletedTask;
    }
}