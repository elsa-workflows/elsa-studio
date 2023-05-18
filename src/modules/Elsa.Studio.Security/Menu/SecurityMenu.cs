using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

public class SecurityMenu : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Security,
                Href = "/security/users",
                Text = "Security",
                GroupName = MenuItemGroups.Settings.Name,
                SubMenuItems =
                {
                    new MenuItem
                    {
                        Text = "Users",
                        Href = "/security/users",
                        Icon = Icons.Material.Filled.Person
                    },
                    new MenuItem
                    {
                        Text = "Roles",
                        Href = "/security/roles",
                        Icon = Icons.Material.Filled.PeopleOutline
                    }
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}