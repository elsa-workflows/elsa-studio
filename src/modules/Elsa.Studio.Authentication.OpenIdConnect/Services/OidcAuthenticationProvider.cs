using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// Implementation of <see cref="IAuthenticationProvider"/> that retrieves tokens from OIDC authentication.
/// </summary>
public class OidcAuthenticationProvider(IOidcTokenAccessor tokenAccessor) : IAuthenticationProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await tokenAccessor.GetAccessTokenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        return await tokenAccessor.GetAccessTokenAsync(scopes, cancellationToken);
    }
}
