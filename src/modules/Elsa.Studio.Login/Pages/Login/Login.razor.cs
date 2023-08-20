using Elsa.Studio.Authentication.JwtBearer.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Pages.Login.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Elsa.Studio.Login.Pages.Login;

/// <summary>
/// The login page.
/// </summary>
[AllowAnonymous]
public partial class Login
{
    private readonly LoginModel _model = new();

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    [Inject] private IJwtAccessor JwtAccessor { get; set; } = default!;
    [Inject] private ICredentialsValidator CredentialsValidator { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState == null)
            return;

        var state = await AuthenticationState;
        if (state.User.Identity?.IsAuthenticated == true)
            NavigationManager.NavigateTo("/");
    }

    private async Task TryLogin()
    {
        var isValid = await ValidateCredentials(_model.Username, _model.Password);
        if (!isValid)
        {
            Snackbar.Add("Invalid credentials. Please try again", Severity.Error);
            return;
        }

        NavigationManager.NavigateTo("/");
    }

    private async Task<bool> ValidateCredentials(string username, string password)
    {
        var result = await CredentialsValidator.ValidateCredentialsAsync(username, password);

        if (!result.IsAuthenticated)
            return false;

        await JwtAccessor.WriteTokenAsync(TokenNames.AccessToken, result.AccessToken!);
        await JwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, result.RefreshToken!);
        return true;
    }
}