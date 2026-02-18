using Elsa.Studio.Contracts;
using Elsa.Studio.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Elsa.Studio.Shared.Services;

/// <summary>
/// Default logout service that navigates to the login page using Blazor's NavigationManager.
/// This provides a smooth SPA transition without full page reloads.
/// </summary>
public class DefaultLogoutService(NavigationManager navigationManager, IJSRuntime jsRuntime, IConfiguration configuration) : ILogoutService
{
    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        try
        {
            // Clear any client-side authentication state if needed
            await ClearClientStateAsync();
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
        var loginPath = configuration.GetValue<string>("Routes:LoginPath") ?? "/login";
        navigationManager.NavigateTo(loginPath, forceLoad: false, replace: true);
    }
    
    private async Task ClearClientStateAsync()
    {
        try
        {
            // Get storage keys from configuration with fallbacks
            var authTokenKey = configuration.GetValue<string>("Authentication:StorageKeys:AuthToken") ?? "authToken";
            
            // Clear localStorage/sessionStorage if used for auth tokens
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", authTokenKey);
            await jsRuntime.InvokeVoidAsync("sessionStorage.clear");
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}