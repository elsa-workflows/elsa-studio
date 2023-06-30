using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.List;

public partial class Index
{
    private MudTable<WorkflowDefinitionRow> _table = null!;
    private HashSet<WorkflowDefinitionRow> _selectedRows = new();
    private int _totalCount;
    private string? _searchString;

    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private async Task<TableData<WorkflowDefinitionRow>> ServerReload(TableState state)
    {
        // TODO: Load only json-based workflow definitions for now.
        // Later, also allow CLR-based workflows to be "edited" (publish / unpublish / position activities / set variables, etc.)

        const string materializerName = "Json";

        var request = new ListWorkflowDefinitionsRequest
        {
            Page = 0,
            PageSize = 15,
            MaterializerName = materializerName
        };

        var latestWorkflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(request, VersionOptions.Latest);
        var unpublishedWorkflowDefinitionIds = latestWorkflowDefinitionsResponse.Items.Where(x => !x.IsPublished).Select(x => x.DefinitionId).ToList();

        var publishedWorkflowDefinitions = await WorkflowDefinitionService.ListAsync(new ListWorkflowDefinitionsRequest
        {
            MaterializerName = materializerName,
            DefinitionIds = unpublishedWorkflowDefinitionIds,
        }, VersionOptions.Published);

        _totalCount = latestWorkflowDefinitionsResponse.TotalCount;

        var latestWorkflowDefinitions = latestWorkflowDefinitionsResponse.Items
            .Select(definition =>
            {
                var latestVersionNumber = definition.Version;
                var isPublished = definition.IsPublished;
                var publishedVersion = isPublished
                    ? definition
                    : publishedWorkflowDefinitions.Items.FirstOrDefault(x => x.DefinitionId == definition.DefinitionId);
                var publishedVersionNumber = publishedVersion?.Version?.ToString() ?? "-";

                return new WorkflowDefinitionRow(definition.DefinitionId, publishedVersionNumber, definition.Name, definition.Description, definition.IsPublished);
            })
            .ToList();

        return new TableData<WorkflowDefinitionRow> { TotalItems = _totalCount, Items = latestWorkflowDefinitions };
    }

    private async Task OnCreateWorkflowClicked()
    {
        var workflowName = await WorkflowDefinitionService.GenerateUniqueNameAsync();
        
        var parameters = new DialogParameters<CreateWorkflowDialog>
        {
            { x => x.WorkflowName, workflowName }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        
        var dialogInstance = await DialogService.ShowAsync<CreateWorkflowDialog>("New workflow", parameters, options);
        var dialogResult = await dialogInstance.Result;

        if (!dialogResult.Canceled)
        {
            var newWorkflowModel = (WorkflowPropertiesModel)dialogResult.Data;
            var workflowDefinition = await WorkflowDefinitionService.CreateNewWorkflowDefinitionAsync(newWorkflowModel.Name!, newWorkflowModel.Description!);
            Edit(workflowDefinition.DefinitionId);
        }
    }

    private void Edit(string definitionId)
    {
        NavigationManager.NavigateTo($"/workflows/definitions/{definitionId}/edit");
    }

    private void OnRowClick(TableRowClickEventArgs<WorkflowDefinitionRow> e)
    {
        Edit(e.Item.DefinitionId);
    }
    
    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox(
            "Delete selected workflows?", 
            "Are you sure you want to delete the selected workflows?", 
            yesText:"Delete", 
            cancelText:"Cancel");
        
        if(result != true)
            return;
        
        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        await WorkflowDefinitionService.BulkDeleteAsync(workflowDefinitionIds);
        Reload();
    }

    private void OnSearch(string text)
    {
        _searchString = text;
        Reload();
    }
    
    private void Reload()
    {
        _table.ReloadServerData();
    }

    private record WorkflowDefinitionRow(string DefinitionId, string Version, string? Name, string? Description, bool IsPublished);
}