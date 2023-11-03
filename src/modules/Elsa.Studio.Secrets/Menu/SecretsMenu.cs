using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Secrets.Menu;

public class SecretsMenu : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Key,
                Href = "secrets",
                Text = "Secrets",
                GroupName = MenuItemGroups.Settings.Name
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}