using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Dashboard.Workflows.Menu;

public class WorkflowsMenu : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.Schema,
                Text = "Workflows",
                GroupName = MenuItemGroups.General.Name,
                SubMenuItems =
                {
                    new MenuItem()
                    {
                        Text = "Definitions",
                        Href = "/workflows/definitions"
                    },
                    new MenuItem()
                    {
                        Text = "Instances",
                        Href = "/workflows/instances"
                    },
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}