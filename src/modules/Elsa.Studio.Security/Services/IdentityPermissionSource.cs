using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Adapts the backend's effective Identity permission snapshot for Core navigation filtering.
/// </summary>
public sealed class IdentityPermissionSource(IIdentityPermissionContext permissionContext) : ICurrentUserPermissionSource
{
    public async ValueTask<IReadOnlySet<string>?> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await permissionContext.GetAsync(cancellationToken);
        if (snapshot.State != IdentityPermissionSnapshotState.Ready)
            return null;

        return snapshot.Grants
            .SelectMany(grant => grant.Value.Select(verb => $"{grant.Key}:{verb}"))
            .ToHashSet(StringComparer.Ordinal);
    }
}
