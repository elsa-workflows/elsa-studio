using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

[UsedImplicitly]
public partial class ApiKeys
{
    private MudTable<ApiKeyModel> _table = null!;
    private HashSet<ApiKeyModel> _selectedRows = new();

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;

    private async Task<IApiKeysApi> GetApiClientAsync()
    {
        return await ApiClientProvider.GetApiAsync<IApiKeysApi>();
    }
    
    private async Task<TableData<ApiKeyModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetApiClientAsync();
        var response = await apiClient.ListAsync(cancellationToken);

        return new TableData<ApiKeyModel>
        {
            TotalItems = (int)response.Count,
            Items = response.Items
        };
    }

    private async Task OnCreateClicked()
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"ai/api-keys/new"));
    }

    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"ai/api-keys/{id}"));
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string id)
    {
        await EditAsync(id);
    }

    private async Task OnRowClick(TableRowClickEventArgs<ApiKeyModel> e)
    {
        await EditAsync(e.Item.Id);
    }

    private async Task OnDeleteClicked(ApiKeyModel model)
    {
        var result = await DialogService.ShowMessageBox("Delete API Key?", "Are you sure you want to delete this API key?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var id = model.Id;
        var apiClient = await GetApiClientAsync();
        await apiClient.DeleteAsync(id);
        Reload();
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBox("Delete Selected API keys?", "Are you sure you want to delete the selected API keys?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var ids = _selectedRows.Select(x => x.Id).ToList();
        var request = new BulkDeleteRequest { Ids = ids };
        var apiClient = await GetApiClientAsync();
        await apiClient.BulkDeleteAsync(request);
        Reload();
    }
}