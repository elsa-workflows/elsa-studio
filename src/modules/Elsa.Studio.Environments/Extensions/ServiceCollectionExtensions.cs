using System.Net.Http.Headers;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Environments.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;

namespace Elsa.Studio.Environments.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the environments module.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEnvironments(this IServiceCollection services)
    {
        services.AddRefitClient<IEnvironmentsClient>().ConfigureHttpClient((sp, client) =>
        {
            var backendAccessor = sp.GetRequiredService<IBackendAccessor>();
            client.BaseAddress = backendAccessor.Backend.Url;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "TODO");
        });

        services.AddSingleton<IEnvironmentService, DefaultEnvironmentService>();
        services.Replace(ServiceDescriptor.Singleton<IBackendConnectionProvider, EnvironmentBackendConnectionProvider>());
        services.AddSingleton<IStartupTask, LoadEnvironmentsStartupTask>();
        services.AddSingleton<IModule, Module>();
        return services;
    }
}