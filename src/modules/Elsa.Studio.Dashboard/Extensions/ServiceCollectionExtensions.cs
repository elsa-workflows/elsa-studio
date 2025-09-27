using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Dashboard.Extensions;

/// <summary>
/// Provides service registration helpers for the dashboard module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dashboard feature and its menu provider.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, DashboardMenu>();
    }
}