using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using Radzen;

namespace Elsa.Studio.Login.Pages.Login;

/// <summary>
/// Ultra-lightweight login page with configuration-driven branding - NO heavy Elsa modules loaded.
/// </summary>
[AllowAnonymous]
public partial class Login : ComponentBase
{
    // Only inject ESSENTIAL services for authentication
    [Inject] private IJwtAccessor JwtAccessor { get; set; } = null!;
    [Inject] private ICredentialsValidator CredentialsValidator { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IUserMessageService UserMessageService { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;

    // Configuration-driven values - NO magic constants
    private string ClientVersion { get; set; } = "Elsa Studio";
    private string ServerVersion { get; set; } = "3.x";
    private string AppName { get; set; } = "Elsa Studio";
    private string AppTagline { get; set; } = "Workflow Management";
    private string LogoUrl { get; set; } = "/logo.png";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        // Load configuration values - lightweight, no API calls
        LoadConfigurationValues();
        
        // NO heavy services, NO API calls, NO Elsa modules
        // Configuration reading is extremely fast and doesn't block rendering
    }

    private void LoadConfigurationValues()
    {
        // Read branding configuration - fast local operation
        var brandingSection = Configuration.GetSection(LoginBrandingOptions.SectionName);
        
        // Set values with fallbacks to avoid null references
        AppName = brandingSection.GetValue<string>("AppName") ?? "Elsa Studio";
        AppTagline = brandingSection.GetValue<string>("AppTagline") ?? "Workflow Management";
        LogoUrl = brandingSection.GetValue<string>("LogoUrl") ?? "/logo.png";
        ClientVersion = brandingSection.GetValue<string>("ClientVersion") ?? "Elsa Studio";
        ServerVersion = brandingSection.GetValue<string>("ServerVersion") ?? "3.x";
    }

    private async Task TryLogin(LoginArgs args)
    {
        try
        {
            var isValid = await ValidateCredentials(args.Username, args.Password);
            if (!isValid)
            {
                UserMessageService.ShowSnackbarTextMessage("Invalid credentials. Please try again", Severity.Error);
                return;
            }

            // Notify authentication state change
            ((AccessTokenAuthenticationStateProvider)AuthenticationStateProvider).NotifyAuthenticationStateChanged();

            // NOW navigate to main app - THIS is where Elsa modules load for the first time
            await NavigateToMainApp();
        }
        catch (Exception ex)
        {
            UserMessageService.ShowSnackbarTextMessage($"Login failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task NavigateToMainApp()
    {
        // Force full page reload to load complete Elsa application with all modules
        var uri = new Uri(NavigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var returnUrl))
        {
            NavigationManager.NavigateTo(returnUrl.FirstOrDefault() ?? "/", forceLoad: true);
        }
        else
        {
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
    }

    private async Task<bool> ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            return false;
        
        var result = await CredentialsValidator.ValidateCredentialsAsync(username, password);

        if (!result.IsAuthenticated)
            return false;

        await JwtAccessor.WriteTokenAsync(TokenNames.AccessToken, result.AccessToken!);
        await JwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, result.RefreshToken!);
        return true;
    }
}