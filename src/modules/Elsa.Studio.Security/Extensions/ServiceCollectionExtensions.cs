using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Security.Extensions;

/// <summary>
/// Provides extension methods for service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the security module.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, SecurityMenu>();
    }
}