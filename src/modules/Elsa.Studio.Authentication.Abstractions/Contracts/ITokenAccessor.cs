namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>
/// Provides access to authentication tokens stored by an authentication provider.
/// This abstraction allows different authentication providers (OIDC, JWT, OAuth2, etc.)
/// to implement token retrieval in their own way.
/// </summary>
public interface ITokenAccessor
{
    /// <summary>
    /// Retrieves the access token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token value, or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the ID token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The ID token value, or null if not available.</returns>
    Task<string?> GetIdTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the refresh token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The refresh token value, or null if not available.</returns>
    Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);
}
