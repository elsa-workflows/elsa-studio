namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Extended OIDC token accessor that supports scope-aware token acquisition.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IOidcTokenAccessor"/> to support requesting tokens
/// with specific scopes, enabling incremental consent and multi-audience scenarios.
/// </remarks>
public interface IOidcTokenAccessorWithScopes : IOidcTokenAccessor
{
    /// <summary>
    /// Gets a token with specific scopes.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve.</param>
    /// <param name="scopes">The specific scopes to request. If null or empty, uses default behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token value, or null if not available.</returns>
    Task<string?> GetTokenAsync(
        string tokenName,
        IEnumerable<string>? scopes,
        CancellationToken cancellationToken = default);
}
