using Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.BlazorServer.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
[Obsolete("Elsa.Studio.Login.* is obsolete. Use Elsa.Studio.Authentication.ElsaAuth.* (or Elsa.Studio.Authentication.OpenIdConnect.*) instead.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds login services with Blazor Server implementations.
    /// </summary>
    [Obsolete("Elsa.Studio.Login.* is obsolete. Use services.AddElsaAuth() from Elsa.Studio.Authentication.ElsaAuth.BlazorServer and optionally add Elsa.Studio.Login UI separately.")]
    public static IServiceCollection AddLoginModule(this IServiceCollection services)
    {
        // Legacy UI + feature registrations.
        services.AddLoginModuleCore();

        // Replace the old platform auth plumbing with ElsaAuth.
        Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Extensions.ServiceCollectionExtensions.AddElsaAuth(services);

        return services;
    }

    /// <summary>
    /// Configures the login module to use OpenIdConnect (OIDC)
    /// </summary>
    public static IServiceCollection UseOpenIdConnect(this IServiceCollection services, Action<OpenIdConnectConfiguration> configure)
    {
        return services
            .UseOpenIdConnectCore(configure)
        ;
    }
}