using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.List;

public partial class Index
{
    private MudTable<WorkflowDefinitionRow> _table = null!;

    private int _totalCount;
    private string? _searchString;

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

    private void New()
    {
        Edit();
    }

    private void Edit(string definitionId)
    {
        NavigationManager.NavigateTo($"/workflows/definitions/{definitionId}/edit");
    }

    private void Edit()
    {
        var definitionId = Guid.NewGuid().ToString("N");
        Edit(definitionId);
    }

    private void RowClick(TableRowClickEventArgs<WorkflowDefinitionRow> e)
    {
        Edit(e.Item.DefinitionId);
    }

    private void OnSearch(string text)
    {
        _searchString = text;
        _table.ReloadServerData();
    }

    private record WorkflowDefinitionRow(string DefinitionId, string Version, string? Name, string? Description, bool IsPublished);
}