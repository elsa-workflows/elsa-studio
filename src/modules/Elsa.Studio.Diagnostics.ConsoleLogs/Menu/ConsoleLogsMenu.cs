using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Menu;

/// <summary>
/// Exposes menu entries for diagnostics console logs.
/// </summary>
public class ConsoleLogsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

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

        return menuItems;
    }
}
