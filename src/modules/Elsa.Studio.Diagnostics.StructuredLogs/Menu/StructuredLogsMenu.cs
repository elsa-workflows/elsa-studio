using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Menu;

/// <summary>
/// Exposes menu entries for structured logs.
/// </summary>
public class StructuredLogsMenu : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.FormatListBulleted,
                Href = "diagnostics/structured-logs",
                Text = "Structured Logs",
                GroupName = MenuItemGroups.Diagnostics.Name
            }
        };

        return menuItems;
    }
}
