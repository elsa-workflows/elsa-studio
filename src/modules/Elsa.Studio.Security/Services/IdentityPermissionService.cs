using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Security.Services;

public interface IIdentityPermissionService
{
    ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads Elsa permission claims to tailor identity-management affordances.</summary>
public sealed class IdentityPermissionService(AuthenticationStateProvider authenticationStateProvider) : IIdentityPermissionService
{
    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await ListAsync(cancellationToken);
        return permissions.Contains("*") || permissions.Contains(permission);
    }

    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        return user.FindAll("permissions").Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
    }
}
