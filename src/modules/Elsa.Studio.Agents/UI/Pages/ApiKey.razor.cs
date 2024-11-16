using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

public partial class ApiKey : StudioComponentBase
{
    /// The ID of the API key to edit.
    [Parameter] public string ApiKeyId { get; set; } = default!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    private bool IsNew => string.Equals("new", ApiKeyId, StringComparison.OrdinalIgnoreCase);

    private MudForm _form = default!;
    private ApiKeyInputModelValidator _validator = default!;
    private ApiKeyModel _apiKey = new();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IApiKeysApi>();

        if (IsNew)
            _apiKey = new();
        else
            _apiKey = await apiClient.GetAsync(ApiKeyId);

        _validator = new ApiKeyInputModelValidator(apiClient);
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        var apiClient = await ApiClientProvider.GetApiAsync<IApiKeysApi>();

        if (IsNew)
        {
            _apiKey = await apiClient.CreateAsync(_apiKey);
            Snackbar.Add("API key successfully created.", Severity.Success);
        }
        else
        {
            _apiKey = await apiClient.UpdateAsync(ApiKeyId, _apiKey);
            Snackbar.Add("API key successfully updated.", Severity.Success);
        }

        StateHasChanged();
        NavigationManager.NavigateTo("/ai/api-keys");
    }
}