using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Menu;

/// <summary>
/// Exposes menu entries for diagnostics console logs.
/// </summary>
public class ConsoleLogsMenu : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Terminal,
                Href = "diagnostics/console",
                Text = "Console",
                GroupName = MenuItemGroups.Diagnostics.Name
            }
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>(menuItems);
    }
}
