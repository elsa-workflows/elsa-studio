using Elsa.Secrets;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.UI.Validators;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Secrets.UI.Pages;

public partial class Secret : StudioComponentBase
{
    /// The ID of the secret to edit.
    [Parameter] public string SecretId { get; set; } = default!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private MudForm _form = default!;
    private SecretInputModelValidator _validator = default!;
    private SecretInputModel _model = new();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<ISecretsApi>();
        _validator = new SecretInputModelValidator(apiClient);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<ISecretsApi>();
        _model = await apiClient.GetInputAsync(SecretId);
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        var apiClient = await ApiClientProvider.GetApiAsync<ISecretsApi>();
        await apiClient.UpdateAsync(SecretId, _model);
        Snackbar.Add("Secret successfully updated.", Severity.Success);
        StateHasChanged();
        NavigationManager.NavigateTo("secrets");
    }
}