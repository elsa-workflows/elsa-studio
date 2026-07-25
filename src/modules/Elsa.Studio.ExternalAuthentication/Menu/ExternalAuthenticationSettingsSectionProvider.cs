using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Settings.Contracts;
using Elsa.Studio.Settings.Models;
using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Menu;

public sealed class ExternalAuthenticationSettingsSectionProvider(
    IRemoteFeatureProvider remoteFeatures,
    IExternalAuthenticationPermissionService permissions) : ISettingsSectionProvider
{
    public async ValueTask<IEnumerable<SettingsSectionDescriptor>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken) ||
            !await permissions.HasAsync(ExternalAuthenticationPermissions.Read, cancellationToken))
            return [];

        return
        [
            new(
                "sso-connections",
                "SSO connections",
                "Configure the identity providers that can authenticate Elsa users.",
                "settings/sso-connections",
                Icons.Material.Filled.AdminPanelSettings,
                100)
        ];
    }
}
