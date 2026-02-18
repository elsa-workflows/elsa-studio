using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Logout service for ElsaIdentity authentication that clears JWT tokens and navigates instantly to login.
/// </summary>
public class ElsaIdentityLogoutService(IJwtAccessor jwtAccessor, NavigationManager navigationManager, IJSRuntime jsRuntime, IConfiguration configuration) : ILogoutService
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
        
        // Use configuration-driven route instead of hardcoded path
        var loginPath = configuration.GetValue<string>("Routes:LoginPath") ?? "/login";
        navigationManager.NavigateTo(loginPath, forceLoad: false, replace: true);
    }
    
    private async Task ClearAdditionalAuthStateAsync()
    {
        try
        {
            // Get storage keys from configuration with fallbacks
            var userKey = configuration.GetValue<string>("Authentication:StorageKeys:User") ?? "user";
            var authExpiryKey = configuration.GetValue<string>("Authentication:StorageKeys:AuthExpiry") ?? "authExpiry";
            
            // Clear any localStorage items that might contain auth state
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", userKey);
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", authExpiryKey);
        }
        catch (JSException)
        {
            // Handle JS errors gracefully
        }
    }
}