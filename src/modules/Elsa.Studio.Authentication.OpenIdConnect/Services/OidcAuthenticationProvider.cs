using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// Implementation of <see cref="IAuthenticationProvider"/> that retrieves tokens from OIDC authentication.
/// </summary>
public class OidcAuthenticationProvider : IAuthenticationProvider
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
}
