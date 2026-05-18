using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;
using Refit;
using System.Net;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Menu;

/// <summary>
/// Exposes menu entries for diagnostics console logs.
/// </summary>
public class ConsoleLogsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsConsoleLogsEnabledAsync(cancellationToken))
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

    private async Task<bool> IsConsoleLogsEnabledAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await remoteFeatureProvider.IsEnabledAsync(Feature.RemoteFeatureName, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return false;
        }
        catch (ApiException)
        {
            return false;
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
