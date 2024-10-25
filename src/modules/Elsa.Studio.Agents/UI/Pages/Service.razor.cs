using System.Text.Json;
using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

public partial class Service : StudioComponentBase
{
    /// The ID of the service to edit.
    [Parameter] public string ServiceId { get; set; } = default!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    private bool IsNew => string.Equals("new", ServiceId, StringComparison.OrdinalIgnoreCase);

    private MudForm _form = default!;
    private ServiceInputModelValidator _validator = default!;
    private ServiceModel _entity = new();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IServicesApi>();

        if (IsNew)
            _entity = new();
        else
            _entity = await apiClient.GetAsync(ServiceId);

        _validator = new ServiceInputModelValidator(apiClient);
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        var apiClient = await ApiClientProvider.GetApiAsync<IServicesApi>();

        if (IsNew)
        {
            _entity = await apiClient.CreateAsync(_entity);
            Snackbar.Add("Service successfully created.", Severity.Success);
        }
        else
        {
            _entity = await apiClient.UpdateAsync(ServiceId, _entity);
            Snackbar.Add("Service successfully updated.", Severity.Success);
        }

        StateHasChanged();
        NavigationManager.NavigateTo("/ai/services");
    }

    private MudBlazor.Converter<IDictionary<string,object>,string> GetSettingsConverter()
    {
        return new MudBlazor.Converter<IDictionary<string, object>, string>
        {
            SetFunc = x => x == null ? "{}" : JsonSerializer.Serialize(x, new JsonSerializerOptions { WriteIndented = true }),
            GetFunc = x => JsonSerializer.Deserialize<IDictionary<string, object>>(x) ?? new Dictionary<string, object>()
        };
    }
}