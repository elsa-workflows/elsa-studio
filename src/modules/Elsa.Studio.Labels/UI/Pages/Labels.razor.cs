using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Components;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Labels.UI.Pages;

[UsedImplicitly]
public partial class Labels
{
    private MudTable<Elsa.Labels.Entities.Label> _table = null!;
    private HashSet<Elsa.Labels.Entities.Label> _selectedRows = new();

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    
    private async Task<ILabelsApi> GetApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<ILabelsApi>();
    }
    
    private async Task<TableData<Elsa.Labels.Entities.Label>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetApiAsync();
        var labels = await apiClient.ListAsync(cancellationToken);

        return new TableData<Elsa.Labels.Entities.Label>
        {
            TotalItems = (int)labels.Count,
            Items = labels.Items
        };
    }

    private async Task OnCreateClicked()
    {
        var apiClient = await GetApiAsync();
        var parameters = new DialogParameters<CreateLabelDialog>
        {
            { x => x.LabelName, "LabelName" }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<CreateLabelDialog>("New Label", parameters, options);
        var dialogResult = await dialogInstance.Result;

        if (!dialogResult!.Canceled)
        {
            var inputModel = (LabelInputModel)dialogResult.Data;
            try
            {
                var model = await apiClient.CreateAsync(inputModel);
                await EditAsync(model.Id);
                Reload();
            }
            catch (Exception e)
            {
                Snackbar.Add(e.Message, Severity.Error);
            }
        }
    }

    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"labels/{id}"));
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    private async Task OnRowClick(TableRowClickEventArgs<Elsa.Labels.Entities.Label> e)
    {
        await EditAsync(e.Item!.Id);
    }

    private async Task OnDeleteClicked(Elsa.Labels.Entities.Label model)
    {
        var result = await DialogService.ShowMessageBox("Delete label?", "Are you sure you want to delete this label?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var id = model.Id;
        var apiClient = await GetApiAsync();
        await apiClient.DeleteAsync(id);
        Reload();
    }

    private Task OnBulkDeleteClicked()
    {
        Snackbar.Add("Not implemented", Severity.Error);
        return Task.CompletedTask;
    }
}