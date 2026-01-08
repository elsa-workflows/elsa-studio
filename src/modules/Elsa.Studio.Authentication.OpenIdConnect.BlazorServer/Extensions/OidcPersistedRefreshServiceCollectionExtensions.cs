using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

/// <summary>
/// Extension methods for enabling persisted OIDC refresh behavior.
/// </summary>
public static class OidcPersistedRefreshServiceCollectionExtensions
{
    /// <summary>
    /// Enables a background ping to the refresh endpoint so the auth cookie can be renewed on a normal HTTP request.
    /// </summary>
    public static IServiceCollection AddOidcPersistedRefreshBackgroundPing(this IServiceCollection services, Action<OidcPersistedRefreshClientOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.AddOptions<OidcPersistedRefreshClientOptions>();

        services.AddHttpClient(OidcPersistedRefreshBackgroundService.ClientName);
        services.AddHostedService<OidcPersistedRefreshBackgroundService>();

        return services;
    }
}
