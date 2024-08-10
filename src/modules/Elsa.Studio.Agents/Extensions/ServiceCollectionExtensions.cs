using Elsa.Studio.Agents.UI.Providers;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Studio.Extensions;

/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
public static class ServiceCollectionExtensions
{
    /// Adds the Agents module.
    public static IServiceCollection AddAgentsModule(this IServiceCollection services)
    {
        services.AddActivityDisplaySettingsProvider<AgentsActivityDisplaySettingsProvider>();
        
        return services;
    }
}