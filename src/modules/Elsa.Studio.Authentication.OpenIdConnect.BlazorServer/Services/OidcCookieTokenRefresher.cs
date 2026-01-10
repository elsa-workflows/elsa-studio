using System.Globalization;
using System.Text.Json;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Refreshes the OIDC access token and persists it by renewing the auth cookie.
/// Intended to be invoked from a normal HTTP endpoint where headers can still be written.
/// </summary>
public class OidcCookieTokenRefresher(
    ISingleFlightCoordinator refreshCoordinator,
    IOidcRefreshConfigurationProvider refreshConfigurationProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<OidcTokenRefreshOptions> refreshOptions)
{
    internal const string AnonymousClientName = "Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Anonymous";

    /// <summary>
    /// Attempts to refresh tokens and renew the cookie if needed.
    /// Returns <c>true</c> if tokens were refreshed and persisted; otherwise <c>false</c>.
    /// </summary>
    public async Task<bool> TryRefreshAndRenewCookieAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var options = refreshOptions.Value;

        if (!options.EnableRefreshTokens)
            return false;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        // Must be able to write cookies.
        if (httpContext.Response.HasStarted)
            return false;

        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAtString = await httpContext.GetTokenAsync("expires_at");

        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(expiresAtString))
            return false;

        if (!DateTimeOffset.TryParse(expiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
            return false;

        if (expiresAt > DateTimeOffset.UtcNow.Add(options.RefreshSkew))
            return false;

        var didRefresh = false;

        await refreshCoordinator.RunAsync(async ct =>
        {
            // Check again under the lock.
            var currentExpiresAtString = await httpContext.GetTokenAsync("expires_at");
            if (!DateTimeOffset.TryParse(currentExpiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var currentExpiresAt))
                return 0;

            if (currentExpiresAt > DateTimeOffset.UtcNow.Add(options.RefreshSkew))
                return 0;

            var refreshConfig = await refreshConfigurationProvider.GetAsync(ct);
            if (refreshConfig == null)
                return 0;

            var httpClient = httpClientFactory.CreateClient(AnonymousClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, refreshConfig.TokenEndpoint);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = refreshConfig.ClientId,
                ["refresh_token"] = refreshToken,
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

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal!, authResult.Properties);

            didRefresh = true;
            return 0;
        }, cancellationToken);

        return didRefresh;
    }
}

