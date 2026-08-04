using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

public sealed class IdentitySecurityMenuContributor(
    IRemoteFeatureProvider remoteFeatures,
    IIdentityPermissionService permissions) : ISecurityMenuContributor
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        var items = new List<MenuItem>();
        if (await permissions.HasAsync(IdentityPermissions.ReadUser, cancellationToken))
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.People,
                Href = "security/users",
                Text = "Users",
                Order = 10
            });
        }

        if (await permissions.HasAsync(IdentityPermissions.ReadRole, cancellationToken))
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.Badge,
                Href = "security/roles",
                Text = "Roles",
                Order = 20
            });
        }

        return items;
    }
}
