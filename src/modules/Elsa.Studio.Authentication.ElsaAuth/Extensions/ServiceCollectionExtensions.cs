using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

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
            .AddScoped<AuthenticationStateProvider, AccessTokenAuthenticationStateProvider>()
            .AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<Unauthorized>>()
            .AddScoped<ICredentialsValidator, ElsaIdentityCredentialsValidator>()
            .AddScoped<IAuthenticationProvider, JwtAuthenticationProvider>()
            .AddScoped<IHttpConnectionOptionsConfigurator, ElsaAuthHttpConnectionOptionsConfigurator>();

            services.AddHttpClient(ElsaIdentityRefreshTokenService.AnonymousClientName);
            services.AddScoped<IRefreshTokenService, ElsaIdentityRefreshTokenService>();
            
        return services;
    }
}
