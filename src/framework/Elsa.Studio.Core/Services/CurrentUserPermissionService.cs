using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Services;

/// <summary>
/// Reads Elsa's <c>permissions</c> claims, or an optional effective-permission source, to tailor Studio affordances.
/// Server-side authorization remains the source of truth.
/// </summary>
public sealed class CurrentUserPermissionService(
    AuthenticationStateProvider? authenticationStateProvider = null,
    ICurrentUserPermissionSource? effectivePermissionSource = null) : ICurrentUserPermissionService
{
    private const string PermissionClaimType = "permissions";

    /// <inheritdoc />
    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (authenticationStateProvider is null)
            return true;

        if (!TryParse(permission, out var required))
            return false;

        var grants = await GetGrantsAsync(cancellationToken);
        if (grants is null)
            return false;

        return grants.Any(grant => TryParse(grant, out var parsedGrant) && Satisfies(parsedGrant, required));
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticationStateProvider is null)
            return new HashSet<string>(StringComparer.Ordinal);

        return await GetGrantsAsync(cancellationToken) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    private async ValueTask<IReadOnlySet<string>?> GetGrantsAsync(CancellationToken cancellationToken)
    {
        var user = (await authenticationStateProvider!.GetAuthenticationStateAsync()).User;

        if (user.Identity?.IsAuthenticated != true)
            return null;

        var claims = user.FindAll(PermissionClaimType).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);

        // Claims are authoritative when present. This preserves the existing claim-based behavior and prevents an
        // effective-permission source from broadening a principal that explicitly carries a narrower grant set.
        if (claims.Count > 0)
            return claims;

        if (effectivePermissionSource is null)
            return null;

        // OIDC principals may carry no Elsa claims because their backend access token is authorized separately.
        // An unavailable source is represented by null and fails closed for authenticated users.
        return await effectivePermissionSource.GetAsync(cancellationToken);
    }

    private static bool TryParse(string? value, out PermissionParts permission)
    {
        permission = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed == "*")
        {
            permission = new("*", "*");
            return true;
        }

        var separator = trimmed.IndexOf(':');
        if (separator <= 0 || separator == trimmed.Length - 1 || trimmed.IndexOf(':', separator + 1) >= 0)
            return false;

        permission = new(trimmed[..separator], trimmed[(separator + 1)..]);
        return true;
    }

    private static bool Satisfies(PermissionParts granted, PermissionParts required) =>
        ResourceMatches(granted.Resource, required.Resource) &&
        (granted.Verb == "*" || string.Equals(granted.Verb, required.Verb, StringComparison.Ordinal));

    private static bool ResourceMatches(string granted, string required)
    {
        if (granted == "*" || string.Equals(granted, required, StringComparison.Ordinal))
            return true;

        if (!granted.EndsWith("/*", StringComparison.Ordinal))
            return false;

        var prefix = granted[..^2];
        return string.Equals(prefix, required, StringComparison.Ordinal) ||
               (required.Length > prefix.Length &&
                required.StartsWith(prefix, StringComparison.Ordinal) &&
                required[prefix.Length] == '/');
    }

    private readonly record struct PermissionParts(string Resource, string Verb);
}
