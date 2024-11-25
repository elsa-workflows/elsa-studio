using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Services;
using MudBlazor;

namespace Elsa.Studio.Workflows.Menu;

public class WorkflowsMenu(ILocalizer localizer) : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.Schema,
                Text = localizer["Workflows"],
                GroupName = MenuItemGroups.General.Name,
                SubMenuItems =
                {
                    new MenuItem()
                    {
                        Text = localizer["Definitions"],
                        Href = "workflows/definitions"
                    },
                    new MenuItem()
                    {
                        Text = localizer["Instances"],
                        Href = "workflows/instances"
                    },
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}