using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Services;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Menu;

public class DashboardMenu : IMenuProvider
{
    private readonly DefaultLocalizer _localizer;
    public DashboardMenu(DefaultLocalizer localizer)
    {
        _localizer = localizer;
    }
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.SpaceDashboard,
                Href = "",
                Text = _localizer["Dashboard"],
                GroupName = MenuItemGroups.General.Name,
                Match = NavLinkMatch.All
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}