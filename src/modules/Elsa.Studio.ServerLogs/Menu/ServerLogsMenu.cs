using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.ServerLogs.Menu;

/// <summary>
/// Exposes menu entries for server logs.
/// </summary>
public class ServerLogsMenu : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Terminal,
                Href = "server-logs",
                Text = "Server Logs",
                GroupName = MenuItemGroups.General.Name
            }
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>(menuItems);
    }
}
