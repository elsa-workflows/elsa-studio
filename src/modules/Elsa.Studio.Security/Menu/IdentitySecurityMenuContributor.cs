using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

public sealed class IdentitySecurityMenuContributor(
    IRemoteFeatureProvider remoteFeatures,
    IRoleAdministrationAccessService roleAccessService) : ISecurityMenuContributor
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        var roleAccess = await roleAccessService.GetAsync(cancellationToken);
        return roleAccess.CanView
            ? [new()
            {
                Icon = Icons.Material.Filled.Badge,
                Href = "security/roles",
                Text = "Roles",
                Order = 20
            }]
            : [];
    }
}
