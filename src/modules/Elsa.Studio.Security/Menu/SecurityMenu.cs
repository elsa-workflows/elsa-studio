using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

/// <summary>
/// Provides permission-aware role administration navigation.
/// </summary>
public class SecurityMenu(IRoleAdministrationAccessService accessService) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var access = await accessService.GetAsync(cancellationToken);
        if (!access.CanView)
            return [];

        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Security,
                Href = "security/roles",
                Text = "Security",
                GroupName = MenuItemGroups.Settings.Name,
                SubMenuItems =
                {
                    new MenuItem
                    {
                        Text = "Roles",
                        Href = "security/roles",
                        Icon = Icons.Material.Filled.PeopleOutline
                    }
                }
            }
        };

        return menuItems;
    }
}
