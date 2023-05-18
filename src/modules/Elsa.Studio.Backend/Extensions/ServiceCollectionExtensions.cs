using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Options;
using Elsa.Studio.Backend.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendModule(this IServiceCollection services, Action<BackendOptions>? configureOptions = default)
    {
        services.Configure(configureOptions ?? (_ => { }));
        
        return services
            .AddSingleton<IBackendAccessor, DefaultBackendAccessor>()
            .AddSingleton<IBackendConnectionProvider, DefaultBackendConnectionProvider>()
            .AddSingleton<IModule, Module>()
            ;
    }
}