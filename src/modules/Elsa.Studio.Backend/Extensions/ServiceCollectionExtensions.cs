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
    public static IServiceCollection AddBackendModule(this IServiceCollection services, Action<BackendOptions>? configureOptions = default)
    {
        services.Configure(configureOptions ?? (_ => { }));
        
        return services
            .AddScoped<IBackendAccessor, DefaultBackendAccessor>()
            .AddScoped<IBackendConnectionProvider, DefaultBackendConnectionProvider>()
            .AddScoped<IModule, Module>()
            ;
    }
}