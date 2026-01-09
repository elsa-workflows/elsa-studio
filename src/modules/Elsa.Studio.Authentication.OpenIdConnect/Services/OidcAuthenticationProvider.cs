using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// Implementation of <see cref="IAuthenticationProvider"/> that retrieves tokens from OIDC authentication.
/// </summary>
public class OidcAuthenticationProvider(IOidcTokenAccessor tokenAccessor) : IAuthenticationProvider, IScopedAccessTokenProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        // Map the token name to OIDC token name conventions
        var oidcTokenName = tokenName switch
        {
            TokenNames.AccessToken => "access_token",
            TokenNames.IdToken => "id_token",
            TokenNames.RefreshToken => "refresh_token",
            _ => tokenName
        };

        return await tokenAccessor.GetTokenAsync(oidcTokenName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        // Map the token name to OIDC token name conventions
        var oidcTokenName = tokenName switch
        {
            TokenNames.AccessToken => "access_token",
            TokenNames.IdToken => "id_token",
            TokenNames.RefreshToken => "refresh_token",
            _ => tokenName
        };

        // All OIDC token accessors now support scoped requests
        var scopedAccessor = (IOidcTokenAccessorWithScopes)tokenAccessor;
        return await scopedAccessor.GetTokenAsync(oidcTokenName, scopes, cancellationToken);
    }
}
