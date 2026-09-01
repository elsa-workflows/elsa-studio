using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Security.Constants;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Combines the remote Identity feature gate with the current caller's effective grants.
/// </summary>
public sealed class RoleAdministrationAccessService(
    IRemoteFeatureProvider remoteFeatureProvider,
    IIdentityPermissionContext permissionContext) : IRoleAdministrationAccessService
{
    public async Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return RoleAdministrationAccess.Unavailable;

        var snapshot = await permissionContext.GetAsync(cancellationToken);
        if (snapshot.State == IdentityPermissionSnapshotState.Unavailable)
            return RoleAdministrationAccess.Unavailable;

        if (snapshot.State == IdentityPermissionSnapshotState.Forbidden ||
            !snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.View))
            return RoleAdministrationAccess.Forbidden;

        return new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready,
            CanView: true,
            CanCreate: snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Create),
            CanUpdate: snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Update),
            CanDelete: snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Delete));
    }

    public void Invalidate() => permissionContext.Invalidate();
}
