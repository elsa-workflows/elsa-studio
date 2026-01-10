namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines a provider for retrieving authentication tokens.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Asynchronously retrieves an access token for authentication purposes.
    /// Uses the default scopes configured for the authentication provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the access token as a string or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves an access token for authentication purposes with specific scopes.
    /// This is primarily used in OIDC scenarios where different API audiences require different scopes.
    /// </summary>
    /// <param name="scopes">The specific scopes to request for this token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the access token as a string or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default);
}