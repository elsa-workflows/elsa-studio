using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Security.Extensions;

/// <summary>
/// Provides extension methods for configuring security services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the security module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, SecurityMenu>();
    }
}