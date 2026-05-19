using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using MudBlazor;
using Refit;
using System.Net;

namespace Elsa.Studio.Secrets.Menu;

public class SecretsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsSecretsEnabledAsync(cancellationToken))
            return [];

        return
        [
            new()
            {
                Icon = Icons.Material.Filled.Key,
                Href = "security/secrets",
                Text = "Secrets",
                GroupName = MenuItemGroups.Settings.Name
            }
        ];
    }

    private async Task<bool> IsSecretsEnabledAsync(CancellationToken cancellationToken)
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
        catch (InvalidOperationException e) when (e.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal))
        {
            return false;
        }
    }
}
