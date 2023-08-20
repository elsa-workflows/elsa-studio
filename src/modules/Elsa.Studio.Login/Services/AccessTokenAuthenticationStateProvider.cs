using System.Security.Claims;
using Elsa.Studio.Authentication.JwtBearer.Contracts;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Provides the authentication state for the current user based on an access token.
/// </summary>
public class AccessTokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJwtAccessor _jwtAccessor;
    private readonly IJwtParser _jwtParser;

    /// <inheritdoc />
    public AccessTokenAuthenticationStateProvider(IJwtAccessor jwtAccessor, IJwtParser jwtParser)
    {
        _jwtAccessor = jwtAccessor;
        _jwtParser = jwtParser;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authToken = await _jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (string.IsNullOrEmpty(authToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = _jwtParser.Parse(authToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }
}