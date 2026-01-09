namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines a provider for retrieving an access token for authentication purposes.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Asynchronously retrieves an access token for authentication purposes.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the access token as a string or null if not available.</returns>
    public Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves an access token for authentication purposes with specific scopes.
    /// </summary>
    /// <param name="tokenName">The name of the token to retrieve.</param>
    /// <param name="scopes">The specific scopes to request for this token. If null or empty, uses default scopes.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the access token as a string or null if not available.</returns>
    public Task<string?> GetAccessTokenAsync(string tokenName, IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        // Default implementation delegates to non-scoped version
        return GetAccessTokenAsync(tokenName, cancellationToken);
    }
}