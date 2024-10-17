using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Refit;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// Displays a list of workflow definitions.
public partial class WorkflowDefinitionList
{
    private MudTable<WorkflowDefinitionRow> _table = null!;
    private HashSet<WorkflowDefinitionRow> _selectedRows = new();
    private long _totalCount;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    private string SearchTerm { get; set; } = string.Empty;
    private bool IsReadOnlyMode { get; set; }
    private const string ReadonlyWorkflowsExcluded = "The read-only workflows will not be affected.";

    private async Task<TableData<WorkflowDefinitionRow>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var request = new ListWorkflowDefinitionsRequest
        {
            IsSystem = false,
            Page = state.Page,
            PageSize = state.PageSize,
            SearchTerm = SearchTerm,
            OrderBy = GetOrderBy(state.SortLabel),
            OrderDirection = state.SortDirection == SortDirection.Descending
                ? OrderDirection.Descending
                : OrderDirection.Ascending
        };

        var latestWorkflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(request, VersionOptions.Latest);
        IsReadOnlyMode = (latestWorkflowDefinitionsResponse?.Links?.Count(l => l.Rel == "bulk-publish") ?? 0) == 0;
        var unpublishedWorkflowDefinitionIds = latestWorkflowDefinitionsResponse.Items.Where(x => !x.IsPublished).Select(x => x.DefinitionId).ToList();

        var publishedWorkflowDefinitions = await WorkflowDefinitionService.ListAsync(new ListWorkflowDefinitionsRequest
        {
            DefinitionIds = unpublishedWorkflowDefinitionIds,
        }, VersionOptions.Published);

        _totalCount = latestWorkflowDefinitionsResponse.TotalCount;

        var workflowDefinitionRows = latestWorkflowDefinitionsResponse.Items
            .Select(definition =>
            {
                var latestVersionNumber = definition.Version;
                var isPublished = definition.IsPublished;
                var publishedVersion = isPublished
                    ? definition
                    : publishedWorkflowDefinitions.Items.FirstOrDefault(x => x.DefinitionId == definition.DefinitionId);
                var publishedVersionNumber = publishedVersion?.Version;

                return new WorkflowDefinitionRow(
                    definition.Id,
                    definition.DefinitionId,
                    latestVersionNumber,
                    publishedVersionNumber,
                    definition.Name,
                    definition.Description,
                    definition.IsPublished,
                    (definition?.Links?.Count(l => l.Rel == "publish") ?? 0) == 0);
            })
            .ToList();

        return new TableData<WorkflowDefinitionRow>
        {
            TotalItems = (int)_totalCount,
            Items = workflowDefinitionRows
        };
    }

    private OrderByWorkflowDefinition? GetOrderBy(string sortLabel)
    {
        return sortLabel switch
        {
            "Name" => OrderByWorkflowDefinition.Name,
            "Version" => OrderByWorkflowDefinition.Version,
            "Created" => OrderByWorkflowDefinition.Created,
            _ => null
        };
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
            var newWorkflowModel = (WorkflowMetadataModel)dialogResult.Data;
            var result = await WorkflowDefinitionService.CreateNewDefinitionAsync(newWorkflowModel.Name!, newWorkflowModel.Description!);

            await result.OnSuccessAsync(definition => EditAsync(definition.DefinitionId));
            result.OnFailed(errors => Snackbar.Add(string.Join(Environment.NewLine, errors.Errors)));
        }
    }

    private async Task EditAsync(string definitionId)
    {
        await EditWorkflowDefinition.InvokeAsync(definitionId);
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    private async Task OnRowClick(TableRowClickEventArgs<WorkflowDefinitionRow> e)
    {
        await EditAsync(e.Item.DefinitionId);
    }

    private async Task OnRunWorkflowClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {

        var request = new ExecuteWorkflowDefinitionRequest
        {
            VersionOptions = VersionOptions.Latest
        };

        var definitionId = workflowDefinitionRow!.DefinitionId;
        await WorkflowDefinitionService.ExecuteAsync(definitionId, request);

        Snackbar.Add("Successfully started workflow", Severity.Success);
    }

    private async Task OnDeleteClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBox("Delete workflow?",
            "Are you sure you want to delete this workflow?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var definitionId = workflowDefinitionRow.DefinitionId;
        await WorkflowDefinitionService.DeleteAsync(definitionId);
        Reload();
    }

    private async Task OnCancelClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBox("Cancel running workflow instances?",
            "Are you sure you want to cancel all running workflow instances of this workflow definition?", yesText: "Yes", cancelText: "No");

        if (result != true)
            return;

        var request = new BulkCancelWorkflowInstancesRequest
        {
            DefinitionVersionId = workflowDefinitionRow.Id
        };
        await WorkflowInstanceService.BulkCancelAsync(request);
        Reload();
    }

    private async Task OnDownloadClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var download = await WorkflowDefinitionService.ExportDefinitionAsync(workflowDefinitionRow.DefinitionId, VersionOptions.Latest);
        var fileName = $"{workflowDefinitionRow.Name.Kebaberize()}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox("Delete selected workflows?",
            $"Are you sure you want to delete the selected workflows? {(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : "")}", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        await WorkflowDefinitionService.BulkDeleteAsync(workflowDefinitionIds);
        Reload();
    }

    private async Task OnBulkPublishClicked()
    {
        var result = await DialogService.ShowMessageBox("Publish selected workflows?",
            $"Are you sure you want to publish the selected workflows? {(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : "")}", yesText: "Publish", cancelText: "Cancel");

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkPublishAsync(workflowDefinitionIds);

        if (response.Published.Count > 0)
        {
            var message = response.Published.Count == 1
                ? "One workflow is published"
                : $"{response.Published.Count} workflows are published";
            Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyPublished.Count > 0)
        {
            var message = response.AlreadyPublished.Count == 1
                ? "One workflow is already published"
                : $"{response.AlreadyPublished.Count} workflows are already published";
            Snackbar.Add(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.UpdatedConsumers.Count > 0)
        {
            var message = response.UpdatedConsumers.Count == 1
                ? "One workflow consuming a published workflow has been updated"
                : $"{response.UpdatedConsumers.Count} workflows consuming published workflows have been updated";
            Snackbar.Add(message, Severity.Info, options =>
            {
                options.SnackbarVariant = Variant.Filled;
                options.VisibleStateDuration = 3000;
            });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? "One workflow is not found"
                : $"{response.NotFound.Count} workflows are not found";
            Snackbar.Add(message, Severity.Warning, options => { options.SnackbarVariant = Variant.Filled; });
        }

        Reload();
    }

    private async Task OnBulkRetractClicked()
    {
        var result = await DialogService.ShowMessageBox("Unpublish selected workflows?",
            $"Are you sure you want to unpublish the selected workflows? {(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : "")}", yesText: "Unpublish", cancelText: "Cancel");

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkRetractAsync(workflowDefinitionIds);

        if (response.Retracted.Count > 0)
        {
            var message = response.Retracted.Count == 1
                ? "One workflow is unpublished"
                : $"{response.Retracted.Count} workflows are unpublished";
            Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyRetracted.Count > 0)
        {
            var message = response.AlreadyRetracted.Count == 1
                ? "One workflow is already unpublished"
                : $"{response.AlreadyRetracted.Count} workflows are already unpublished";
            Snackbar.Add(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? "One workflow is not found"
                : $"{response.NotFound.Count} workflows are not found";
            Snackbar.Add(message, Severity.Warning, options => { options.SnackbarVariant = Variant.Filled; });
        }

        Reload();
    }

    private async Task OnBulkExportClicked()
    {
        var workflowVersionIds = _selectedRows.Select(x => x.Id).ToList();
        var download = await WorkflowDefinitionService.BulkExportDefinitionsAsync(workflowVersionIds);
        var fileName = download.FileName;
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private Task OnImportClicked()
    {
        return DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files) await Mediator.NotifyAsync(new ImportingFile(file));
        var maxAllowedSize = 1024 * 1024 * 10; // 10 MB
        var streamParts = files.Select(x => new StreamPart(x.OpenReadStream(maxAllowedSize), x.Name, x.ContentType)).ToList();
        var count = await WorkflowDefinitionService.ImportFilesAsync(streamParts);
        var message = count == 1 ? "Successfully imported one workflow" : $"Successfully imported {count} workflows";
        foreach (var file in files) await Mediator.NotifyAsync(new ImportedFile(file));
        Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }

    private void OnSearchTermChanged(string text)
    {
        if (SearchTerm != text)
        {
            SearchTerm = text;
            Reload();
        }
    }

    private async Task OnPublishClicked(string definitionId)
    {
        var response = await WorkflowDefinitionService.PublishAsync(definitionId);
        if (response.AlreadyPublished)
        {
            Snackbar.Add("Workflow was already published", Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }
        else
        {
            Snackbar.Add("Workflow published", Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.ConsumingWorkflowCount > 0)
        {
            var message = response.ConsumingWorkflowCount == 1
                ? "One workflow consuming a published workflow has been updated"
                : $"{response.ConsumingWorkflowCount} workflows consuming published workflows have been updated";
            Snackbar.Add(message, Severity.Info, options =>
            {
                options.SnackbarVariant = Variant.Filled;
                options.VisibleStateDuration = 3000;
            });
        }

        Reload();
    }

    private async Task OnRetractClicked(string definitionId)
    {
        await WorkflowDefinitionService.RetractAsync(definitionId);
        Snackbar.Add("Workflow retracted", Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }

    private record WorkflowDefinitionRow(
        string Id,
        string DefinitionId,
        int LatestVersion,
        int? PublishedVersion,
        string? Name,
        string? Description,
        bool IsPublished,
        bool IsReadOnlyMode);
}
