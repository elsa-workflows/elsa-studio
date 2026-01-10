using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.Abstractions.Extensions;

/// <summary>
/// Extension methods for registering shared authentication infrastructure.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared authentication infrastructure services.
    /// </summary>
    public static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services)
    {
        // No services to register at this level anymore.
        return services;
    }
}
