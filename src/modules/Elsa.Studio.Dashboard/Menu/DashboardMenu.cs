using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Menu;

public class DashboardMenu : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.SpaceDashboard,
                Href = "/",
                Text = "Dashboard",
                GroupName = MenuItemGroups.General.Name,
                Match = NavLinkMatch.All
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}