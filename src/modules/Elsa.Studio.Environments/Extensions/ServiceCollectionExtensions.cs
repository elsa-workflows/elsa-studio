using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Options;
using Elsa.Studio.Environments.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Elsa.Studio.Environments.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the environments module.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An optional action to configure the options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEnvironments(this IServiceCollection services, Action<WorkflowsEnvironmentOptions>? configureOptions = default)
    {
        services.AddOptions<WorkflowsEnvironmentOptions>();
        Action<WorkflowsEnvironmentOptions> configure = _ => { };

        if(configureOptions != null)
            configure += configureOptions;
        
        services.Configure(configure);

        services.AddRefitClient<IPrimaryServerClient>().ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WorkflowsEnvironmentOptions>>().Value;
            client.BaseAddress = options.PrimaryServerUrl;
        });

        services.AddSingleton<IEnvironmentService, DefaultEnvironmentService>();
        services.AddSingleton<IModule, Module>();
        return services;
    }
}