using Elsa.Agents;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.DomInterop.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Refit;

namespace Elsa.Studio.Agents.UI.Pages;

public partial class Agents
{
    private MudTable<AgentModel> _table = null!;
    private HashSet<AgentModel> _selectedRows = new();

    /// An event that is invoked when an agent is edited.
    [Parameter] public EventCallback<string> EditAgent { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IAgentsApi AgentsApi { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;

    private async Task<TableData<AgentModel>> ServerReload(TableState state)
    {
        var agents = await InvokeWithBlazorServiceContext(() => AgentsApi.ListAsync());

        return new TableData<AgentModel>
        {
            TotalItems = (int)agents.Count,
            Items = agents.Items
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

    private async Task OnCreateAgentClicked()
    {
        var workflowName = await AgentsApi.GenerateUniqueNameAsync();

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
            var result = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.CreateNewDefinitionAsync(newWorkflowModel.Name!, newWorkflowModel.Description!));

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

    private async Task OnDeleteClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBox("Delete workflow?",
            "Are you sure you want to delete this workflow?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var definitionId = workflowDefinitionRow.DefinitionId;
        await InvokeWithBlazorServiceContext((Func<Task>)(() => WorkflowDefinitionService.DeleteAsync(definitionId)));
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
        await InvokeWithBlazorServiceContext(() => WorkflowInstanceService.BulkCancelAsync(request));
        Reload();
    }

    private async Task OnDownloadClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var download = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.ExportDefinitionAsync(workflowDefinitionRow.DefinitionId, VersionOptions.Latest));
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
        await InvokeWithBlazorServiceContext((Func<Task>)(() => WorkflowDefinitionService.BulkDeleteAsync(workflowDefinitionIds)));
        Reload();
    }

    private async Task OnBulkPublishClicked()
    {
        var result = await DialogService.ShowMessageBox("Publish selected workflows?",
            $"Are you sure you want to publish the selected workflows? {(_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : "")}", yesText: "Publish", cancelText: "Cancel");

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.BulkPublishAsync(workflowDefinitionIds));

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
        var response = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.BulkRetractAsync(workflowDefinitionIds));

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
        var download = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.BulkExportDefinitionsAsync(workflowVersionIds));
        var fileName = download.FileName;
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private Task OnImportClicked()
    {
        return DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        var maxAllowedSize = 1024 * 1024 * 10; // 10 MB
        var streamParts = files.Select(x => new StreamPart(x.OpenReadStream(maxAllowedSize), x.Name, x.ContentType)).ToList();
        var count = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.ImportFilesAsync(streamParts));
        var message = count == 1 ? "Successfully imported one workflow" : $"Successfully imported {count} workflows";
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
        var response = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.PublishAsync(definitionId));
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
        await InvokeWithBlazorServiceContext((Func<Task>)(() => WorkflowDefinitionService.RetractAsync(definitionId)));
        Snackbar.Add("Workflow retracted", Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }
}