using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Menu;

/// <summary>
/// Exposes menu entries for OpenTelemetry diagnostics.
/// </summary>
public class OpenTelemetryMenu : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<MenuItem> menuItems =
        [
            new()
            {
                Icon = Icons.Material.Filled.Timeline,
                Href = "diagnostics/opentelemetry",
                Text = "OpenTelemetry",
                GroupName = MenuItemGroups.Diagnostics.Name
            }
        ];

        return ValueTask.FromResult(menuItems);
    }
}
