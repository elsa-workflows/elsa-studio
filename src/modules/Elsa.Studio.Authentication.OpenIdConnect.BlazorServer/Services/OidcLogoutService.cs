using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Logout service for OpenID Connect authentication that navigates smoothly to the logout endpoint.
/// </summary>
public class OidcLogoutService(NavigationManager navigationManager, IJSRuntime jsRuntime) : ILogoutService
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
        
        // Use replace: true for instant, seamless navigation to OIDC logout endpoint
        navigationManager.NavigateTo("/authentication/logout", forceLoad: false, replace: true);
    }
    
    private async Task ClearClientAuthStateAsync()
    {
        try
        {
            // Clear any client-side authentication state
            await jsRuntime.InvokeVoidAsync("sessionStorage.clear");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "oidc.user");
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}