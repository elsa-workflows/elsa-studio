using System.Security.Cryptography;
using System.Text;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// In-memory implementation of <see cref="IScopedTokenCache"/>.
/// </summary>
/// <remarks>
/// For production scenarios with multiple servers, consider implementing
/// a distributed cache version (e.g., using IDistributedCache).
/// </remarks>
public class MemoryScopedTokenCache : IScopedTokenCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultSkew = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryScopedTokenCache"/> class.
    /// </summary>
    public MemoryScopedTokenCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<CachedToken?> GetAsync(string userKey, string scopeKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userKey, scopeKey);

        if (_cache.TryGetValue<CachedToken>(cacheKey, out var token))
        {
            // Check if token is still valid (with skew)
            if (token.ExpiresAt > DateTimeOffset.UtcNow.Add(DefaultSkew))
            {
                return Task.FromResult<CachedToken?>(token);
            }

            // Token expired, remove from cache
            _cache.Remove(cacheKey);
        }

        return Task.FromResult<CachedToken?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string userKey, string scopeKey, CachedToken token, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userKey, scopeKey);

        // Cache until token expiration
        var cacheExpiration = token.ExpiresAt - DateTimeOffset.UtcNow;
        if (cacheExpiration > TimeSpan.Zero)
        {
            _cache.Set(cacheKey, token, cacheExpiration);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates a cache key from user and scope identifiers.
    /// </summary>
    private static string GetCacheKey(string userKey, string scopeKey)
    {
        return $"scoped_token:{userKey}:{scopeKey}";
    }

    /// <summary>
    /// Normalizes scopes into a stable key (sorted, space-separated, hashed).
    /// </summary>
    public static string NormalizeScopeKey(IEnumerable<string> scopes)
    {
        var sortedScopes = scopes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sortedScopes.Count == 0)
            return "default";

        var scopeString = string.Join(" ", sortedScopes);

        // Hash for consistent key length
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(scopeString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
