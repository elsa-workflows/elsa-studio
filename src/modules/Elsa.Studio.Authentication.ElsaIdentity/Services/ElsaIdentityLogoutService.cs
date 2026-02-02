using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Logout service for ElsaIdentity authentication that clears JWT tokens and navigates instantly to login.
/// </summary>
public class ElsaIdentityLogoutService(IJwtAccessor jwtAccessor, NavigationManager navigationManager, IJSRuntime jsRuntime) : ILogoutService
{
    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        try
        {
            // Clear all JWT tokens first
            await jwtAccessor.ClearTokenAsync(TokenNames.AccessToken);
            await jwtAccessor.ClearTokenAsync(TokenNames.RefreshToken);
            
            // Clear any additional client-side auth state
            await ClearAdditionalAuthStateAsync();
        }
        catch (JSDisconnectedException)
        {
            // Handle case where JS runtime is not available
        }
        catch (Exception)
        {
            // Continue with navigation even if token cleanup fails
        }
        
        // Use replace: true for instant, seamless navigation without history entry
        navigationManager.NavigateTo("/login", forceLoad: false, replace: true);
    }
    
    private async Task ClearAdditionalAuthStateAsync()
    {
        try
        {
            // Clear any localStorage items that might contain auth state
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "user");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authExpiry");
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}