using Elsa.Studio.Authentication.Abstractions.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.ComponentProviders;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Models;
using Elsa.Studio.Authentication.ElsaAuth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.ElsaAuth.Extensions;

/// <summary>
/// Service registration extensions for the ElsaAuth authentication module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ElsaAuth core services (provider-agnostic); call one of the <c>Use*</c> methods to select an auth flow.
    /// </summary>
    public static IServiceCollection AddElsaAuthCore(this IServiceCollection services)
    {
        services
            .AddOptions()
            .AddAuthorizationCore()
            .AddAuthenticationInfrastructure()
            .AddScoped<AuthenticationStateProvider, AccessTokenAuthenticationStateProvider>()
            .AddScoped<IUnauthorizedComponentProvider, DefaultUnauthorizedComponentProvider>()
            .AddScoped<ICredentialsValidator, ElsaIdentityCredentialsValidator>()
            .AddScoped<IAuthenticationProvider, JwtAuthenticationProvider>();

            services.AddHttpClient(ElsaIdentityRefreshTokenService.AnonymousClientName);
            services.AddScoped<IRefreshTokenService, ElsaIdentityRefreshTokenService>();

        // Default token claims mapping.
        services.TryAddSingleton<IConfigureOptions<IdentityTokenOptions>, DefaultIdentityTokenOptionsSetup>();

        return services;
    }
}
