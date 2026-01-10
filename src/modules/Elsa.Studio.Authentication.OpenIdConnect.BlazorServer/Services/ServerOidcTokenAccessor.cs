using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of <see cref="IOidcTokenAccessor"/> that retrieves tokens from the authenticated HTTP context.
/// </summary>
public class ServerOidcTokenAccessor : IOidcTokenAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenRefreshCoordinator _refreshCoordinator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<OidcTokenRefreshOptions> _refreshOptions;
    private readonly IOidcRefreshConfigurationProvider _refreshConfigurationProvider;
    private readonly OidcCookieTokenRefresher _cookieTokenRefresher;
    private readonly IScopedTokenCache _scopedTokenCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerOidcTokenAccessor"/> class.
    /// </summary>
    public ServerOidcTokenAccessor(
        IHttpContextAccessor httpContextAccessor,
        ITokenRefreshCoordinator refreshCoordinator,
        IHttpClientFactory httpClientFactory,
        IOptions<OidcTokenRefreshOptions> refreshOptions,
        IOidcRefreshConfigurationProvider refreshConfigurationProvider,
        OidcCookieTokenRefresher cookieTokenRefresher,
        IScopedTokenCache scopedTokenCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _refreshCoordinator = refreshCoordinator;
        _httpClientFactory = httpClientFactory;
        _httpClientFactory = httpClientFactory;
        _refreshOptions = refreshOptions;
        _refreshConfigurationProvider = refreshConfigurationProvider;
        _cookieTokenRefresher = cookieTokenRefresher;
        _scopedTokenCache = scopedTokenCache;
    }

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return GetAccessTokenAsync(scopes: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        // If scopes are provided, acquire scope-specific token
        var scopeArray = scopes?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        
        if (scopeArray?.Length > 0)
        {
            return await GetScopedAccessTokenAsync(httpContext, scopeArray, cancellationToken);
        }

        // Otherwise, use existing cookie token refresh flow
        var options = _refreshOptions.Value;

        if (options is { EnableRefreshTokens: true, Strategy: OidcTokenRefreshStrategy.Persisted })
        {
            // In Persisted mode, try to renew the cookie if this is a normal HTTP request.
            // During Blazor circuit activity, Response.HasStarted is typically true and renewal will be skipped.
            await _cookieTokenRefresher.TryRefreshAndRenewCookieAsync(httpContext, cancellationToken);
        }
        else
        {
            await TryRefreshAccessTokenAsync(httpContext, cancellationToken);
        }

        // Retrieve the token from the authentication properties.
        return await httpContext.GetTokenAsync("access_token");
    }

    /// <inheritdoc />
    public async Task<string?> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        return await httpContext.GetTokenAsync("id_token");
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        return await httpContext.GetTokenAsync("refresh_token");
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
        var cachedToken = await _scopedTokenCache.GetAsync(userKey, scopeKey, cancellationToken);
        if (cachedToken != null)
            return cachedToken.AccessToken;

        // Acquire new token with specific scopes
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var refreshConfig = await _refreshConfigurationProvider.GetAsync(cancellationToken);
        if (refreshConfig == null)
            return null;

        // Use coordinator to prevent concurrent requests for same scope set
        var lockKey = $"{userKey}:{scopeKey}";
        string? newToken = null;

        await _refreshCoordinator.RunAsync(async ct =>
        {
            // Re-check cache after acquiring lock
            var cachedAfterLock = await _scopedTokenCache.GetAsync(userKey, scopeKey, ct);
            if (cachedAfterLock != null)
            {
                newToken = cachedAfterLock.AccessToken;
                return 0;
            }

            // Request token with explicit scopes
            var httpClient = _httpClientFactory.CreateClient("Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Anonymous");

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
            await _scopedTokenCache.SetAsync(userKey, scopeKey, new CachedToken
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt
            }, ct);

            newToken = accessToken;
            return 0;
        }, cancellationToken);

        return newToken;
    }

    private async Task TryRefreshAccessTokenAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var options = _refreshOptions.Value;

        if (!options.EnableRefreshTokens)
            return;

        // BestEffort refresh can only persist tokens by renewing the cookie.
        // In Persisted mode, cookie renewal is performed via the /authentication/refresh endpoint.
        if (options.Strategy != OidcTokenRefreshStrategy.BestEffort)
            return;

        // If headers are already sent, we cannot renew the cookie without throwing.
        if (httpContext.Response.HasStarted)
            return;

        // SaveTokens must be enabled or tokens won't be in the auth cookie.
        var accessToken = await httpContext.GetTokenAsync("access_token");
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAtString = await httpContext.GetTokenAsync("expires_at");

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(expiresAtString))
            return;

        if (!DateTimeOffset.TryParse(expiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
            return;

        if (expiresAt > DateTimeOffset.UtcNow.Add(options.RefreshSkew))
            return; // still valid

        await _refreshCoordinator.RunAsync(async ct =>
        {
            // Re-check after acquiring the lock.
            var currentExpiresAtString = await httpContext.GetTokenAsync("expires_at");
            if (!DateTimeOffset.TryParse(currentExpiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var currentExpiresAt))
                return 0;

            if (currentExpiresAt > DateTimeOffset.UtcNow.Add(options.RefreshSkew))
                return 0;

            var refreshConfig = await _refreshConfigurationProvider.GetAsync(ct);
            if (refreshConfig == null)
                return 0;

            var httpClient = _httpClientFactory.CreateClient("Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Anonymous");

            using var request = new HttpRequestMessage(HttpMethod.Post, refreshConfig.TokenEndpoint);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = refreshConfig.ClientId,
                ["refresh_token"] = refreshToken,
                // client_secret is optional depending on provider/client type.
                ["client_secret"] = refreshConfig.ClientSecret ?? string.Empty
            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)));

            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return 0;

            var payload = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(payload);

            var newAccessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var newRefreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 0;

            if (string.IsNullOrWhiteSpace(newAccessToken) || expiresInSeconds <= 0)
                return 0;

            var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

            var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded)
                return 0;

            authResult.Properties.UpdateTokenValue("access_token", newAccessToken);
            authResult.Properties.UpdateTokenValue("expires_at", newExpiresAt.ToString("o", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(newRefreshToken))
                authResult.Properties.UpdateTokenValue("refresh_token", newRefreshToken);

            // Re-issue the cookie with the updated tokens.
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal!, authResult.Properties);

            return 0;
        }, cancellationToken);
    }
}
