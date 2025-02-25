using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

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
    [Inject] private NavigationManager navigation { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionImporter WorkflowDefinitionImporter { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILocalizer _localizer { get; set; } = default!;
    private string SearchTerm { get; set; } = string.Empty;
    private bool IsReadOnlyMode { get; set; }
    private string ReadonlyWorkflowsExcluded => _localizer["The read-only workflows will not be affected."];

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

        var dialogInstance = await DialogService.ShowAsync<CreateWorkflowDialog>(_localizer["New workflow"], parameters, options);
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
        var response = await WorkflowDefinitionService.ExecuteAsync(definitionId, request);

        if (response.CannotStart)
        {
            Snackbar.Add(_localizer["The workflow cannot be started"], Severity.Error);
            return;
        }

        Snackbar.Add(_localizer["Successfully started workflow"], Severity.Success);
    }

    private async Task OnDeleteClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBox(_localizer["Delete workflow?"],
            _localizer["Are you sure you want to delete this workflow?"], yesText: _localizer["Delete"], cancelText: _localizer["Cancel"]);

        if (result != true)
            return;

        var definitionId = workflowDefinitionRow.DefinitionId;
        await WorkflowDefinitionService.DeleteAsync(definitionId);
        Reload();
    }

    private async Task OnCancelClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBox(_localizer["Cancel running workflow instances?"],
            _localizer["Are you sure you want to cancel"], yesText: _localizer["Yes"], cancelText: _localizer["No"]);

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
        var result = await DialogService.ShowMessageBox(
    _localizer["Delete selected workflows?"],
    _localizer["Are you sure you want to delete the selected workflows?"] +(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""),yesText: _localizer["Delete"],cancelText: _localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        await WorkflowDefinitionService.BulkDeleteAsync(workflowDefinitionIds);
        Reload();
    }

    private async Task OnBulkPublishClicked()
    {
        var result = await DialogService.ShowMessageBox(_localizer["Publish selected workflows?"],
            _localizer["Are you sure you want to publish the selected workflows?"]+ (_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""), yesText: _localizer["Publish"], cancelText: _localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkPublishAsync(workflowDefinitionIds);

        if (response.Published.Count > 0)
        {
            var message = response.Published.Count == 1
                ? _localizer["One workflow is published"]
                : $"{response.Published.Count}"+ _localizer["workflows are published"];
            Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyPublished.Count > 0)
        {
            var message = response.AlreadyPublished.Count == 1
                ? _localizer["One workflow is already published"]
                : $"{response.AlreadyPublished.Count}"+ _localizer["workflows are already published"];
            Snackbar.Add(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.UpdatedConsumers.Count > 0)
        {
            var message = response.UpdatedConsumers.Count == 1
                ? _localizer["One workflow consuming a published workflow has been updated"]
                : $"{response.UpdatedConsumers.Count}"+ _localizer["workflows consuming published workflows have been updated"];
            Snackbar.Add(message, Severity.Info, options =>
            {
                options.SnackbarVariant = Variant.Filled;
                options.VisibleStateDuration = 3000;
            });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? _localizer["One workflow is not found"]
                : $"{response.NotFound.Count}"+ _localizer["workflows are not found"];
            Snackbar.Add(message, Severity.Warning, options => { options.SnackbarVariant = Variant.Filled; });
        }

        Reload();
    }

    private async Task OnBulkRetractClicked()
    {
        var result = await DialogService.ShowMessageBox(_localizer["Unpublish selected workflows?"],
            _localizer["Are you sure you want to unpublish the selected workflows?"] +(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""), yesText: _localizer["Unpublish"], cancelText: _localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkRetractAsync(workflowDefinitionIds);

        if (response.Retracted.Count > 0)
        {
            var message = response.Retracted.Count == 1
                ? _localizer["One workflow is unpublished"]
                : $"{response.Retracted.Count}"+ _localizer["workflows are unpublished"];
            Snackbar.Add(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyRetracted.Count > 0)
        {
            var message = response.AlreadyRetracted.Count == 1
                ? _localizer["One workflow is already unpublished"]
                : $"{response.AlreadyRetracted.Count}"+ _localizer["workflows are already unpublished"];
            Snackbar.Add(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? _localizer["One workflow is not found"]
                : $"{response.NotFound.Count}"+ _localizer["workflows are not found"];
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
        var results = (await WorkflowDefinitionImporter.ImportFilesAsync(files)).ToList();
        var successfulResultCount = results.Count(x => x.IsSuccess);
        var failedResultCount = results.Count(x => !x.IsSuccess);
        var successfulWorkflowsTerm = successfulResultCount == 1 ? "workflow" : "workflows";
        var failedWorkflowsTerm = failedResultCount == 1 ? "workflow" : "workflows";
        var message = results.Count == 0 ? _localizer["No workflows found to import."] :
            successfulResultCount > 0 && failedResultCount == 0 ? $"{successfulResultCount} {successfulWorkflowsTerm}"+ _localizer["imported successfully."] :
            successfulResultCount == 0 && failedResultCount > 0 ? _localizer["Failed to import"]+" {failedResultCount} {failedWorkflowsTerm}." : $"{successfulResultCount} {successfulWorkflowsTerm}"+ _localizer["imported successfully."]+" {failedResultCount} {failedWorkflowsTerm}"+ _localizer["failed to import."];
        var severity = results.Count == 0 ? Severity.Info : successfulResultCount > 0 && failedResultCount > 0 ? Severity.Warning : failedResultCount == 0 ? Severity.Success : Severity.Error;
        Snackbar.Add(message, severity, options =>
        {
            options.SnackbarVariant = Variant.Filled;
            options.CloseAfterNavigation = failedResultCount > 0;
            options.VisibleStateDuration = failedResultCount > 0 ? 10000 : 3000;
        });
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
            Snackbar.Add(_localizer["Workflow was already published"], Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }
        else
        {
            Snackbar.Add(_localizer["Workflow published"], Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.ConsumingWorkflowCount > 0)
        {
            var message = response.ConsumingWorkflowCount == 1
                ? _localizer["One workflow consuming a published workflow has been updated"]
                : $"{response.ConsumingWorkflowCount}"+ _localizer["workflows consuming published workflows have been updated"];
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
        Snackbar.Add(_localizer["Workflow retracted"], Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
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