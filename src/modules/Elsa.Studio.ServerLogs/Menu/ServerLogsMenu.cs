using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.ServerLogs.Menu;

/// <summary>
/// Exposes menu entries for server logs.
/// </summary>
public class ServerLogsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsServerLogStreamingEnabledAsync(cancellationToken))
            return [];

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

        return menuItems;
    }

    private async Task<bool> IsServerLogStreamingEnabledAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await remoteFeatureProvider.IsEnabledAsync(Feature.RemoteFeatureName, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return false;
        }
    }
}
