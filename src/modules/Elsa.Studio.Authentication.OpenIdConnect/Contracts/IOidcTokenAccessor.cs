namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Provides access to OIDC tokens stored in the authentication context.
/// Extends <see cref="ITokenAccessor"/> with OIDC-specific functionality for scope-aware token acquisition.
/// </summary>
public interface IOidcTokenAccessor
{
    /// <summary>
    /// Gets an access token with specific scopes.
    /// This enables multi-audience scenarios where different tokens are needed for different APIs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token value, or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
