using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// Implementation of <see cref="IAuthenticationProvider"/> that retrieves tokens from OIDC authentication.
/// </summary>
public class OidcAuthenticationProvider : IAuthenticationProvider, IScopedAccessTokenProvider
{
    private readonly IOidcTokenAccessor _tokenAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcAuthenticationProvider"/> class.
    /// </summary>
    public OidcAuthenticationProvider(IOidcTokenAccessor tokenAccessor)
    {
        _tokenAccessor = tokenAccessor;
    }

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

        return await _tokenAccessor.GetTokenAsync(oidcTokenName, cancellationToken);
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

        // If the accessor supports scoped token requests, use it
        if (_tokenAccessor is IOidcTokenAccessorWithScopes scopedAccessor)
        {
            return await scopedAccessor.GetTokenAsync(oidcTokenName, scopes, cancellationToken);
        }

        // Fall back to non-scoped accessor for backward compatibility
        return await _tokenAccessor.GetTokenAsync(oidcTokenName, cancellationToken);
    }
}
