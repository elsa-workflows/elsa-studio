using Elsa.Studio.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Providers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Studio.Extensions;

/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
public static class ServiceCollectionExtensions
{
    /// Adds the Agents module.
    public static IServiceCollection AddAgentsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
                .AddScoped<IFeature, Feature>()
                .AddScoped<IMenuProvider, AgentsMenu>()
                .AddScoped<IMenuGroupProvider, AgentsMenu>()
                .AddRemoteApi<IAgentsApi>(backendApiConfig)
                .AddActivityDisplaySettingsProvider<AgentsActivityDisplaySettingsProvider>()
            ;
    }
}