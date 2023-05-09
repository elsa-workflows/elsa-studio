using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Dashboard.Services;

public class DefaultMenuProvider : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Dashboard,
                Href = "/",
                Text = "Dashboard",
                GroupName = MenuItemGroups.General.Name,
                Match = NavLinkMatch.All
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}