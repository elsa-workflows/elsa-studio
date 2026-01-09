namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>
/// Provides access tokens with specific scopes for different purposes.
/// </summary>
/// <remarks>
/// This interface extends authentication providers to support scope-aware token acquisition,
/// allowing different tokens for different API audiences (e.g., Graph vs. backend API).
/// </remarks>
public interface IScopedAccessTokenProvider
{
    /// <summary>
    /// Gets an access token for the specified token name and scopes.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve (e.g., "access_token").</param>
    /// <param name="scopes">The specific scopes to request for this token. If null or empty, uses default scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token, or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(
        string tokenName,
        IEnumerable<string>? scopes,
        CancellationToken cancellationToken = default);
}
