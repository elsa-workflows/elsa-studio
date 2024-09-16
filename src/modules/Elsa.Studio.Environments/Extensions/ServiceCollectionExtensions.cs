using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
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
    public static IServiceCollection AddEnvironmentsModule(this IServiceCollection services)
    {
        services.AddScoped<IEnvironmentService, DefaultEnvironmentService>();
        services.Replace(ServiceDescriptor.Scoped<IBackendApiClientProvider, EnvironmentBackendApiClientProvider>());
        //services.AddSingleton<IStartupTask, LoadEnvironmentsStartupTask>();
        services.AddScoped<IFeature, Feature>();
        return services;
    }
}