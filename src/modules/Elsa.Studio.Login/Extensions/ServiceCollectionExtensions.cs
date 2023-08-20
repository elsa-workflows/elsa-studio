using Elsa.Api.Client.Contracts;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Services;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.ComponentProviders;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

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
            .AddOptions()
            .AddAuthorizationCore()
            .AddScoped<AuthenticatingApiHttpMessageHandler>()
            .AddScoped<IHttpMessageHandlerProvider, AuthenticatingHttpMessageHandlerProvider>()
            .AddScoped<AuthenticationStateProvider, AccessTokenAuthenticationStateProvider>()
            .AddScoped<IModule, Module>()
            .AddScoped<IUnauthorizedComponentProvider, RedirectToLoginUnauthorizedComponentProvider>()
            .AddScoped<ICredentialsValidator, DefaultCredentialsValidator>()
            .AddScoped<IRemoteBackendApiClientProvider, DefaultRemoteBackendApiClientProvider>()
            ;
    }
}