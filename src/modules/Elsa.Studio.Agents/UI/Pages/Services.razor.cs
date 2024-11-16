using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

[UsedImplicitly]
public partial class Services
{
    private MudTable<ServiceModel> _table = null!;
    private HashSet<ServiceModel> _selectedRows = new();

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;

    private async Task<IServicesApi> GetApiClientAsync()
    {
        return await ApiClientProvider.GetApiAsync<IServicesApi>();
    }
    
    private async Task<TableData<ServiceModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetApiClientAsync();
        var response = await apiClient.ListAsync(cancellationToken);

        return new TableData<ServiceModel>
        {
            TotalItems = (int)response.Count,
            Items = response.Items
        };
    }

    private async Task OnCreateClicked()
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"/ai/services/new"));
    }

    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"/ai/services/{id}"));
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string id)
    {
        await EditAsync(id);
    }

    private async Task OnRowClick(TableRowClickEventArgs<ServiceModel> e)
    {
        await EditAsync(e.Item.Id);
    }

    private async Task OnDeleteClicked(ServiceModel model)
    {
        var result = await DialogService.ShowMessageBox("Delete Service?", "Are you sure you want to delete this service?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var id = model.Id;
        var apiClient = await GetApiClientAsync();
        await apiClient.DeleteAsync(id);
        Reload();
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox("Delete Selected services?", "Are you sure you want to delete the selected services?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var ids = _selectedRows.Select(x => x.Id).ToList();
        var request = new BulkDeleteRequest { Ids = ids };
        var apiClient = await GetApiClientAsync();
        await apiClient.BulkDeleteAsync(request);
        Reload();
    }
}