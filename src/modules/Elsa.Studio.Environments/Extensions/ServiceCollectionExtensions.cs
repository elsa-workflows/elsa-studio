using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Environments.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the environments module.
    /// </summary>
    /// <remarks>
    /// Environment switching updates <see cref="IRemoteBackendAccessor"/> so
    /// <see cref="DefaultBackendApiClientProvider"/> keeps minting clients.
    /// Environments load from <see cref="Feature.InitializeAsync"/> after the app shell has a
    /// circuit and user context. They are not registered as <see cref="IStartupTask"/> so Blazor
    /// Server host startup does not call the environments API (or JS interop) during
    /// <c>Host.StartAsync</c>.
    /// <see cref="IRemoteFeatureProvider.ListAsync"/> waits for
    /// <see cref="EnvironmentLoader.EnsureLoadedAsync"/> so the default environment is selected
    /// before the remote feature catalog is fetched.
    /// </remarks>
    public static IServiceCollection AddEnvironmentsModule(this IServiceCollection services, BackendApiConfig? backendApiConfig = null)
    {
        services.AddScoped<IEnvironmentService, DefaultEnvironmentService>();
        services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, EnvironmentRemoteBackendAccessor>());
        services.AddScoped<EnvironmentLoader>();
        services.AddScoped<IFeature, Feature>();
        services.AddRemoteApi<IEnvironmentsClient>(backendApiConfig);
        DecorateRemoteFeatureProvider(services);
        return services;
    }

    private static void DecorateRemoteFeatureProvider(IServiceCollection services)
    {
        services.TryAddScoped<RemoteFeatureProvider>();
        services.Replace(ServiceDescriptor.Scoped<IRemoteFeatureProvider>(sp =>
            new EnvironmentAwareRemoteFeatureProvider(
                sp.GetRequiredService<RemoteFeatureProvider>(),
                sp.GetRequiredService<EnvironmentLoader>())));

        // DefaultFeatureService resolves a single IRemoteFeatureProvider (last registration).
        // Workflows may add another after this module; wrap whatever is resolved at that point.
        services.Replace(ServiceDescriptor.Scoped<IFeatureService>(sp =>
        {
            var loader = sp.GetRequiredService<EnvironmentLoader>();
            var remote = sp.GetService<IRemoteFeatureProvider>()
                ?? sp.GetRequiredService<RemoteFeatureProvider>();
            if (remote is not EnvironmentAwareRemoteFeatureProvider)
                remote = new EnvironmentAwareRemoteFeatureProvider(remote, loader);

            return new DefaultFeatureService(sp.GetServices<IFeature>(), remote);
        }));
    }
}
