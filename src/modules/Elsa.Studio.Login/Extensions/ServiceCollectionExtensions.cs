using Elsa.Studio.Contracts;
using Elsa.Studio.Login.ComponentProviders;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Login.Models;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Login.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the login module to the service collection.
    /// </summary>
    public static IServiceCollection AddLoginModuleCore(this IServiceCollection services)
    {
        return services
                .AddScoped<IFeature, LoginFeature>()
                .AddOptions()
                .AddAuthorizationCore()
                .AddScoped<AuthenticatingApiHttpMessageHandler>()
                .AddScoped<AuthenticationStateProvider, AccessTokenAuthenticationStateProvider>()
                .AddScoped<IUnauthorizedComponentProvider, RedirectToLoginUnauthorizedComponentProvider>()
            ;
    }

    /// <summary>
    /// Configures the login module to use elsa identity
    /// </summary>
    public static IServiceCollection UseElsaIdentity(this IServiceCollection services)
    {
        return services
                .AddScoped<ICredentialsValidator, ElsaIdentityCredentialsValidator>()
                .AddScoped<IAuthorizationService, ElsaIdentityAuthorizationService>()
                .AddScoped<IRefreshTokenService, ElsaIdentityRefreshTokenService>()
                .AddScoped<IEndSessionService, ElsaIdentityEndSessionService>()
            ;
    }

    /// <summary>
    /// Configures the service collection to use OAuth2 for credential validation and related services.
    /// </summary>
    public static IServiceCollection UseOAuth2(this IServiceCollection services, Action<OAuth2CredentialsValidatorOptions> configure)
    {
        services.Configure(configure);
        
        services.AddHttpClient<OAuth2HttpClient>(httpClient =>
        {
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<OAuth2CredentialsValidatorOptions>>().Value;
            httpClient.BaseAddress = new(options.TokenEndpoint);
        });
        
        return services
                .AddScoped<ICredentialsValidator, OAuth2CredentialsValidator>()
                .AddScoped<IAuthorizationService, ElsaIdentityAuthorizationService>()
                .AddScoped<IRefreshTokenService, ElsaIdentityRefreshTokenService>()
                .AddScoped<IEndSessionService, ElsaIdentityEndSessionService>()
            ;
    }

    /// <summary>
    /// Configures the login module to use OpenIdConnect (OIDC)
    /// </summary>
    public static IServiceCollection UseOpenIdConnect(this IServiceCollection services, Action<OpenIdConnectConfiguration> configure)
    {
        services.Configure(configure);

        return services
                .AddScoped<IAuthorizationService, OpenIdConnectAuthorizationService>()
                .AddScoped<IRefreshTokenService, OpenIdConnectRefreshTokenService>()
                .AddScoped<IEndSessionService, OpenIdConnectEndSessionService>()
            ;
    }
}