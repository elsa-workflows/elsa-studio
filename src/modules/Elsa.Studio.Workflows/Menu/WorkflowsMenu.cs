using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Menu;

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
                        Href = "workflows/definitions"
                    },
                    new MenuItem()
                    {
                        Text = "Instances",
                        Href = "workflows/instances"
                    },
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}