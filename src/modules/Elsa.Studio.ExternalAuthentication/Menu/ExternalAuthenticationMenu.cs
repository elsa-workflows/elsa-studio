using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Menu;

/// <summary>Provides the Identity Provider Connections entry under Studio security settings.</summary>
public sealed class ExternalAuthenticationMenu(IRemoteFeatureProvider remoteFeatures, IExternalAuthenticationPermissionService permissions) : IMenuProvider
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        var items = new List<MenuItem>();
        if (await permissions.HasAsync(ExternalAuthenticationPermissions.Read, cancellationToken))
        {
            items.Add(new MenuItem
            {
                Icon = Icons.Material.Filled.AdminPanelSettings,
                Href = "security/external-authentication",
                Text = "Identity Provider Connections",
                GroupName = MenuItemGroups.Settings.Name
            });
        }
        if (await permissions.HasAsync(ExternalAuthenticationPermissions.ManageLinks, cancellationToken))
        {
            items.Add(new MenuItem
            {
                Icon = Icons.Material.Filled.Link,
                Href = "security/external-authentication/identity-links",
                Text = "External Identity Links",
                GroupName = MenuItemGroups.Settings.Name
            });
        }
        if (await permissions.HasAsync(ExternalAuthenticationPermissions.SessionsRead, cancellationToken))
        {
            items.Add(new MenuItem
            {
                Icon = Icons.Material.Filled.Devices,
                Href = "security/external-authentication/sessions",
                Text = "External Authentication Sessions",
                GroupName = MenuItemGroups.Settings.Name
            });
        }
        return items;
    }
}
