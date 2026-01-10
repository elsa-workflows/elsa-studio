using System.Security.Claims;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of <see cref="ITokenProvider"/> that retrieves tokens from the authenticated HTTP context.
/// </summary>
public class ServerTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    ISingleFlightCoordinator refreshCoordinator,
    OidcOptions oidcOptions,
    TokenRefreshService tokenRefreshService,
    IScopedTokenCache scopedTokenCache)
    : ITokenProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        var scopes = oidcOptions.BackendApiScopes;
        var scopeArray = scopes.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        
        return await GetScopedAccessTokenAsync(httpContext, scopeArray, cancellationToken);
    }
    
    /// <summary>
    /// Acquires a scope-specific access token using refresh token grant with explicit scopes.
    /// </summary>
    private async Task<string?> GetScopedAccessTokenAsync(HttpContext httpContext, string[] scopes, CancellationToken cancellationToken)
    {
        // Get user identifier for cache key
        var userKey = httpContext.User.FindFirstValue("sub") 
                      ?? httpContext.User.FindFirstValue("oid") 
                      ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrWhiteSpace(userKey))
            return null;

        // Generate scope key for cache
        var scopeKey = MemoryScopedTokenCache.NormalizeScopeKey(scopes);

        // Check cache first
        var cachedToken = await scopedTokenCache.GetAsync(userKey, scopeKey, cancellationToken);
        if (cachedToken != null)
            return cachedToken.AccessToken;

        // Acquire new token with specific scopes
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        // Use coordinator to prevent concurrent requests for same scope set.
        string? newToken = null;

        await refreshCoordinator.RunAsync(async ct =>
        {
            // Re-check cache after acquiring lock
            var cachedAfterLock = await scopedTokenCache.GetAsync(userKey, scopeKey, ct);
            if (cachedAfterLock != null)
            {
                newToken = cachedAfterLock.AccessToken;
                return 0;
            }

            // Use the shared token refresh service
            var result = await tokenRefreshService.RefreshTokenAsync(refreshToken, scopes, ct);

            if (!result.Success)
                return 0;

            // Cache the token
            await scopedTokenCache.SetAsync(userKey, scopeKey, new()
            {
                AccessToken = result.AccessToken!,
                ExpiresAt = result.ExpiresAt
            }, ct);

            newToken = result.AccessToken;
            return 0;
        }, cancellationToken);

        return newToken;
    }
}
