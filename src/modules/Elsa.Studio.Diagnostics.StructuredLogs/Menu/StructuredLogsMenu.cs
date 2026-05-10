using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Menu;

/// <summary>
/// Exposes menu entries for structured logs.
/// </summary>
public class StructuredLogsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsStructuredLogsEnabledAsync(cancellationToken))
            return [];

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

    private async Task<bool> IsStructuredLogsEnabledAsync(CancellationToken cancellationToken)
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
