using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.ComponentProviders;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using OidcAuthProvider = Elsa.Studio.Authentication.OpenIdConnect.Services.OidcAuthenticationProvider;
using Elsa.Studio.Authentication.Abstractions.Extensions;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;

/// <summary>
/// Extension methods for configuring OpenID Connect authentication in Blazor WebAssembly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenID Connect authentication services for Blazor WebAssembly.
    /// </summary>
    /// <remarks>
    /// Named to avoid ambiguity with Microsoft.Extensions.DependencyInjection.WebAssemblyAuthenticationServiceCollectionExtensions.AddOidcAuthentication.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for OIDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddElsaOidcAuthentication(
        this IServiceCollection services,
        Action<OidcOptions> configure)
    {
        var options = new OidcOptions();
        configure(options);

        // Register the token accessor
        services.AddScoped<IOidcTokenAccessor, WasmOidcTokenAccessor>();
        services.AddScoped<IAuthenticationProvider, OidcAuthProvider>();

        // Configure WASM authentication using the built-in framework
        services.AddOidcAuthentication(wasmOptions =>
        {
            // Configure the authentication provider options
            wasmOptions.ProviderOptions.Authority = options.Authority;
            wasmOptions.ProviderOptions.ClientId = options.ClientId;
            wasmOptions.ProviderOptions.ResponseType = options.ResponseType;

            // Configure scopes
            foreach (var scope in options.Scopes)
            {
                wasmOptions.ProviderOptions.DefaultScopes.Add(scope);
            }

            // Set redirect URIs
            wasmOptions.ProviderOptions.RedirectUri = options.CallbackPath;
            wasmOptions.ProviderOptions.PostLogoutRedirectUri = options.SignedOutCallbackPath;

            if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
            {
                wasmOptions.ProviderOptions.MetadataUrl = options.MetadataAddress;
            }
        });

        // Provide an OIDC-aware unauthorized component.
        services.AddScoped<IUnauthorizedComponentProvider, OidcUnauthorizedComponentProvider>();

        // Shared auth infrastructure (e.g. delegating handlers).
        services.AddAuthenticationInfrastructure();

        return services;
    }

    /// <summary>
    /// Adds OpenID Connect authentication services for Blazor WebAssembly.
    /// </summary>
    [Obsolete("Use AddElsaOidcAuthentication instead to avoid ambiguity with Microsoft.Extensions.DependencyInjection.WebAssemblyAuthenticationServiceCollectionExtensions.AddOidcAuthentication.")]
    public static IServiceCollection AddOidcAuthentication(
        this IServiceCollection services,
        Action<OidcOptions> configure) => services.AddElsaOidcAuthentication(configure);
}
