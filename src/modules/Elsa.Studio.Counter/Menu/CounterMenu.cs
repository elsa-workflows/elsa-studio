using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Counter.Menu;

public class CounterMenu : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Add,
                Href = "counter",
                Text = "Counter",
                GroupName = MenuItemGroups.General.Name,
                SubMenuItems = new List<MenuItem>
                {
                    new()
                    {
                        Href = "counter",
                        Text = "Sub menu item 1",
                    },
                    new()
                    {
                        Href = "counter2",
                        Text = "Sub menu item 2",
                    },
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}