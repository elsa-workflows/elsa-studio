using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Host.Wasm;

/// <summary>
/// Temporary authentication state provider for testing purposes.
/// </summary>
public class TestAuthStateProvider : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "John Doe"),
            new(ClaimTypes.Role, "Administrator")
        };
        var anonymous = new ClaimsIdentity(claims, "testAuthType");
        return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(anonymous)));
    }
}