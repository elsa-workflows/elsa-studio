using System.Security.Claims;
using System.Text.Json;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of <see cref="IOidcTokenAccessor"/> that retrieves tokens from the authenticated HTTP context.
/// </summary>
public class ServerOidcTokenAccessor(
    IHttpContextAccessor httpContextAccessor,
    ISingleFlightCoordinator refreshCoordinator,
    IHttpClientFactory httpClientFactory,
    OidcOptions oidcOptions,
    IOidcRefreshConfigurationProvider refreshConfigurationProvider,
    IScopedTokenCache scopedTokenCache)
    : IOidcTokenAccessor
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
        var userKey = httpContext.User.FindFirstValue("sub") ?? httpContext.User.FindFirstValue("oid") ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
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

        var refreshConfig = await refreshConfigurationProvider.GetAsync(cancellationToken);
        if (refreshConfig == null)
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

            // Request token with explicit scopes
            var httpClient = httpClientFactory.CreateClient("Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Anonymous");

            using var request = new HttpRequestMessage(HttpMethod.Post, refreshConfig.TokenEndpoint);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = refreshConfig.ClientId,
                ["refresh_token"] = refreshToken,
                ["scope"] = string.Join(" ", scopes),
                ["client_secret"] = refreshConfig.ClientSecret ?? string.Empty
            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)));

            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return 0;

            var payload = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(payload);

            var accessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 0;

            if (string.IsNullOrWhiteSpace(accessToken) || expiresInSeconds <= 0)
                return 0;

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

            // Cache the token
            await scopedTokenCache.SetAsync(userKey, scopeKey, new()
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt
            }, ct);

            newToken = accessToken;
            return 0;
        }, cancellationToken);

        return newToken;
    }
}
