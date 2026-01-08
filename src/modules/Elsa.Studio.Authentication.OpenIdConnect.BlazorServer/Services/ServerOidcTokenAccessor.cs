using System.Globalization;
using System.Text.Json;
using Elsa.Studio.Authentication.Abstractions.Contracts;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerOidcTokenAccessor"/> class.
    /// </summary>
    public ServerOidcTokenAccessor(
        IHttpContextAccessor httpContextAccessor,
        ITokenRefreshCoordinator refreshCoordinator,
        IHttpClientFactory httpClientFactory,
        IOptions<OidcTokenRefreshOptions> refreshOptions,
        IOidcRefreshConfigurationProvider refreshConfigurationProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _refreshCoordinator = refreshCoordinator;
        _httpClientFactory = httpClientFactory;
        _refreshOptions = refreshOptions;
        _refreshConfigurationProvider = refreshConfigurationProvider;
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        // Ensure we have a fresh access token when asked for one.
        if (string.Equals(tokenName, "access_token", StringComparison.Ordinal))
            await TryRefreshAccessTokenAsync(httpContext, cancellationToken);

        // Retrieve the token from the authentication properties.
        return await httpContext.GetTokenAsync(tokenName);
    }

    private async Task TryRefreshAccessTokenAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var options = _refreshOptions.Value;

        if (!options.EnableRefreshTokens)
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
