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

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerOidcTokenAccessor"/> class.
    /// </summary>
    public ServerOidcTokenAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        // Retrieve the token from the authentication properties
        // These are stored when SaveTokens = true in the OIDC options
        return await httpContext.GetTokenAsync(tokenName);
    }
}
