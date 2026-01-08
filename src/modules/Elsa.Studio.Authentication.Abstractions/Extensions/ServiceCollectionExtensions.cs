using Elsa.Studio.Authentication.Abstractions.HttpMessageHandlers;
using Elsa.Studio.Authentication.Abstractions.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        // Used by API clients to attach access tokens.
        services.TryAddScoped<BearerTokenHttpMessageHandler>();
        services.TryAddScoped<AuthenticatingApiHttpMessageHandler>();

        // Used by modules (e.g. Workflows) to retrieve tokens without depending on a specific auth provider.
        services.TryAddScoped<IAuthenticationProviderManager, DefaultAuthenticationProviderManager>();

        return services;
    }
}
