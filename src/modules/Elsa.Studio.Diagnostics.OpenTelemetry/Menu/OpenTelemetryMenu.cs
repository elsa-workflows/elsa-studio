using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;
using Refit;
using System.Net;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Menu;

/// <summary>
/// Exposes menu entries for OpenTelemetry diagnostics.
/// </summary>
public class OpenTelemetryMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsOpenTelemetryEnabledAsync(cancellationToken))
            return [];

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

        return menuItems;
    }

    private async Task<bool> IsOpenTelemetryEnabledAsync(CancellationToken cancellationToken)
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
        catch (InvalidOperationException e) when (IsJavaScriptInteropUnavailable(e))
        {
            return false;
        }
    }

    private static bool IsJavaScriptInteropUnavailable(InvalidOperationException e) =>
        e.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal);
}
