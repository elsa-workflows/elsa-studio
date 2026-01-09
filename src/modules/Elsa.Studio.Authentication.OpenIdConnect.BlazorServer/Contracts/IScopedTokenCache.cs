namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;

/// <summary>
/// Cache for storing scope-specific access tokens.
/// </summary>
/// <remarks>
/// This allows different tokens for different API audiences (e.g., Graph vs. backend API)
/// without overwriting the cookie's primary access token.
/// </remarks>
public interface IScopedTokenCache
{
    /// <summary>
    /// Gets a cached token for the specified user and scope set.
    /// </summary>
    /// <param name="userKey">User identifier (e.g., "sub" or "oid" claim).</param>
    /// <param name="scopeKey">Normalized scope key (sorted, hashed).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached token information, or null if not found or expired.</returns>
    Task<CachedToken?> GetAsync(string userKey, string scopeKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a token for the specified user and scope set.
    /// </summary>
    /// <param name="userKey">User identifier (e.g., "sub" or "oid" claim).</param>
    /// <param name="scopeKey">Normalized scope key (sorted, hashed).</param>
    /// <param name="token">Token information to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(string userKey, string scopeKey, CachedToken token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a cached token with its expiration.
/// </summary>
public sealed class CachedToken
{
    /// <summary>
    /// The access token value.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// When the token expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
