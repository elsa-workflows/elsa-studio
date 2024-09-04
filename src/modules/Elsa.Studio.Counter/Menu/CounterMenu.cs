using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Counter.Menu;

/// <summary>
/// Provides the menu items for the Counter module.
/// </summary>
public class CounterMenu : IMenuProvider
{
    /// <inheritdoc />
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
                        Text = "Counter",
                    }
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}