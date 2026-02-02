using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Shared.Services;

/// <summary>
/// Default logout service that navigates to the login page using Blazor's NavigationManager.
/// This provides a smooth SPA transition without full page reloads.
/// </summary>
public class DefaultLogoutService(NavigationManager navigationManager, IJSRuntime jsRuntime) : ILogoutService
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
        
        // Use replace: true for instant, seamless navigation without history entry
        navigationManager.NavigateTo("/login", forceLoad: false, replace: true);
    }
    
    private async Task ClearClientStateAsync()
    {
        try
        {
            // Clear localStorage/sessionStorage if used for auth tokens
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await jsRuntime.InvokeVoidAsync("sessionStorage.clear");
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}