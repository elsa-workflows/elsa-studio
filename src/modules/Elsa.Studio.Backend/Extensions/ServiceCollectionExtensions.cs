using Elsa.Api.Client.Extensions;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Options;
using Elsa.Studio.Backend.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Backend.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the backend module to the service collection.
    /// </summary>
    public static IServiceCollection AddRemoteBackendModule(this IServiceCollection services, Action<BackendOptions>? configureBackendOptions = default)
    {
        services.Configure(configureBackendOptions ?? (_ => { }));

        // Add the Elsa API client.
        services.AddElsaClient();
        
        return services
            .AddScoped<IRemoteBackendAccessor, DefaultRemoteBackendAccessor>()
            .AddScoped<IRemoteBackendApiClientProvider, DefaultRemoteBackendApiClientProvider>()
            .AddScoped<IModule, Module>()
            ;
    }
}