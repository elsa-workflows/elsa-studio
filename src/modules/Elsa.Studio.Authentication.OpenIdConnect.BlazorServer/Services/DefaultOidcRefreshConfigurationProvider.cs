using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Default implementation that resolves the token endpoint from the configured <see cref="OpenIdConnectOptions"/>
/// and OIDC metadata, with optional overrides from <see cref="OidcTokenRefreshOptions"/>.
/// </summary>
public class DefaultOidcRefreshConfigurationProvider(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    IOptions<OidcTokenRefreshOptions> refreshOptions) : IOidcRefreshConfigurationProvider
{
    /// <inheritdoc />
    public async ValueTask<OidcRefreshConfiguration?> GetAsync(CancellationToken cancellationToken = default)
    {
        var options = oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var overrides = refreshOptions.Value;

        var clientId = overrides.ClientId ?? options.ClientId;
        var clientSecret = overrides.ClientSecret ?? options.ClientSecret;

        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        // Determine the token endpoint.
        var tokenEndpoint = overrides.TokenEndpoint;

        if (string.IsNullOrWhiteSpace(tokenEndpoint))
        {
            // Best practice: use the handler's configuration manager to fetch metadata.
            var configurationManager = options.ConfigurationManager;

            if (configurationManager != null)
            {
                var config = await configurationManager.GetConfigurationAsync(cancellationToken);
                tokenEndpoint = config?.TokenEndpoint;
            }
        }

        if (string.IsNullOrWhiteSpace(tokenEndpoint))
            return null;

        return new(tokenEndpoint, clientId, clientSecret);
    }
}
