using System.Diagnostics;
using System.Security.Claims;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <inheritdoc />
public class JwtAuthenticationProvider(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    ITokenRefreshCoordinator refreshCoordinator,
    IRefreshTokenService refreshTokenService) : IAuthenticationProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        // Only the access token participates in refresh.
        if (!string.Equals(tokenName, TokenNames.AccessToken, StringComparison.Ordinal))
            return await jwtAccessor.ReadTokenAsync(tokenName);

        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        if (!IsExpiredOrNearExpiry(accessToken))
            return accessToken;

        // Single-flight refresh: multiple concurrent API calls shouldn't trigger multiple refresh requests.
        var refreshResponse = await refreshCoordinator.RunAsync(refreshTokenService.RefreshTokenAsync, cancellationToken);

        if (!refreshResponse.IsAuthenticated)
        {
            // Refresh failed: clear local tokens so the app can transition to unauthenticated state.
            await jwtAccessor.ClearTokenAsync(TokenNames.AccessToken);
            await jwtAccessor.ClearTokenAsync(TokenNames.RefreshToken);
            await jwtAccessor.ClearTokenAsync(TokenNames.IdToken);
            return null;
        }

        return await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
    }

    private bool IsExpiredOrNearExpiry(string jwt)
    {
        try
        {
            var claims = jwtParser.Parse(jwt).ToList();
            var expString = claims.FirstOrDefault(x => x.Type == "exp")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(expString) || !long.TryParse(expString, out var exp))
                return false;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
            return expiresAt <= DateTimeOffset.UtcNow.Add(RefreshSkew);
        }
        catch
        {
            // If parsing fails, don't attempt refresh here.
            return false;
        }
    }
}
