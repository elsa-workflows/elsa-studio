namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>
/// Provides access to authentication tokens stored by an authentication provider.
/// This abstraction allows different authentication providers (OIDC, JWT, OAuth2, etc.)
/// to implement token retrieval in their own way.
/// </summary>
public interface ITokenAccessor
{
    /// <summary>
    /// Retrieves an authentication token by name.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve (e.g., "access_token", "id_token", "refresh_token").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The token value, or null if not available.</returns>
    Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default);
}
