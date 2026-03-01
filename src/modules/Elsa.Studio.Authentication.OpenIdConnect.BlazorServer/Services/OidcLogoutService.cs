using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Logout service for OpenID Connect authentication that navigates smoothly to the logout endpoint.
/// </summary>
public class OidcLogoutService(NavigationManager navigationManager, IJSRuntime jsRuntime, IConfiguration configuration) : ILogoutService
{
    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        try
        {
            // Clear any client-side auth state before OIDC logout
            await ClearClientAuthStateAsync();
        }
        catch (JSDisconnectedException)
        {
            // Handle case where JS runtime is not available
        }
        catch (Exception)
        {
            // Continue with navigation even if cleanup fails
        }
        
        // Use configuration-driven route instead of hardcoded path
        var logoutPath = configuration.GetValue<string>("Routes:AuthenticationLogoutPath") ?? "/authentication/logout";
        navigationManager.NavigateTo(logoutPath, forceLoad: false, replace: true);
    }
    
    private async Task ClearClientAuthStateAsync()
    {
        try
        {
            // Get storage key from configuration with fallback
            var oidcUserKey = configuration.GetValue<string>("Authentication:StorageKeys:OidcUser") ?? "oidc.user";
            
            // Clear any client-side authentication state
            await jsRuntime.InvokeVoidAsync("sessionStorage.clear");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", oidcUserKey);
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}