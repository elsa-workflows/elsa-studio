//using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Services;

/// <summary>
/// Temporary authentication state provider for testing purposes.
/// </summary>
public class DefaultAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJwtAccessor _jwtAccessor;
    private readonly IJwtParser _jwtParser;

    public DefaultAuthenticationStateProvider(IJwtAccessor jwtAccessor, IJwtParser jwtParser)
    {
        _jwtAccessor = jwtAccessor;
        _jwtParser = jwtParser;
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authToken = await _jwtAccessor.ReadTokenAsync();

        if (string.IsNullOrEmpty(authToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = _jwtParser.Parse(authToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }
}