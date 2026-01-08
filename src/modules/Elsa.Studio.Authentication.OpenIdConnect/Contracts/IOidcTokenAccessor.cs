namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Provides access to OIDC tokens stored in the authentication context.
/// </summary>
public interface IOidcTokenAccessor
{
    /// <summary>
    /// Retrieves an authentication token by name.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve (e.g., "access_token", "id_token", "refresh_token").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The token value, or null if not available.</returns>
    Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default);
}
