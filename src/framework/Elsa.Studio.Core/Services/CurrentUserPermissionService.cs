using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Services;

/// <summary>
/// Reads Elsa's authoritative <c>permissions</c> claims to tailor Studio affordances.
/// </summary>
public sealed class CurrentUserPermissionService(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserPermissionService
{
    private const string PermissionClaimType = "permissions";

    /// <inheritdoc />
    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!TryParse(permission, out var required))
            return false;

        var grants = await ListAsync(cancellationToken);
        return grants.Any(grant => TryParse(grant, out var parsedGrant) && Satisfies(parsedGrant, required));
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        return user.FindAll(PermissionClaimType).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
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
