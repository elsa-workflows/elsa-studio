using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Helpers;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// Implementation of <see cref="IAuthenticationProvider"/> that retrieves tokens from OIDC authentication.
/// </summary>
public class OidcAuthenticationProvider(IOidcTokenAccessor tokenAccessor) : IAuthenticationProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        var oidcTokenName = TokenNameMapper.MapToOidcTokenName(tokenName);
        return await tokenAccessor.GetTokenAsync(oidcTokenName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        var oidcTokenName = TokenNameMapper.MapToOidcTokenName(tokenName);

        // All OIDC token accessors now support scoped requests
        var scopedAccessor = (IOidcTokenAccessorWithScopes)tokenAccessor;
        return await scopedAccessor.GetTokenAsync(oidcTokenName, scopes, cancellationToken);
    }
}
