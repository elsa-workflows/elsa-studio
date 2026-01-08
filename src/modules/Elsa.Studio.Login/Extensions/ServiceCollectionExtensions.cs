using Elsa.Studio.Authentication.ElsaAuth.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.ComponentProviders;
using Elsa.Studio.Login.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the login module to the service collection.
    /// </summary>
    [Obsolete("Elsa.Studio.Login is obsolete. Use Elsa.Studio.Authentication.ElsaAuth (or Elsa.Studio.Authentication.OpenIdConnect for OIDC) instead.")]
    public static IServiceCollection AddLoginModuleCore(this IServiceCollection services)
    {
        // Keep legacy UI/feature registrations.
        services
            .AddScoped<IFeature, LoginFeature>()
            .AddScoped<IUnauthorizedComponentProvider, RedirectToLoginUnauthorizedComponentProvider>();

        // Delegate the authentication plumbing to ElsaAuth.
        services.AddElsaAuthCore();

        return services;
    }

    /// <summary>
    /// Configures the login module to use elsa identity.
    /// </summary>
    [Obsolete("Elsa.Studio.Login is obsolete. Use services.AddElsaAuthCore().UseElsaIdentityAuth() instead.")]
    public static IServiceCollection UseElsaIdentity(this IServiceCollection services) => services.UseElsaIdentityAuth();

    /// <summary>
    /// Configures the service collection to use OAuth2 for credential validation and related services.
    /// </summary>
    [Obsolete("OAuth2 support via Elsa.Studio.Login is obsolete. ElsaAuth no longer contains OAuth2. If you need OAuth2, keep using the legacy Elsa.Studio.Login OAuth2 implementation or migrate to a dedicated future OAuth2 module.")]
    public static IServiceCollection UseOAuth2(this IServiceCollection services, Action<OAuth2CredentialsValidatorOptions> configure)
    {
        _ = configure;

        throw new NotSupportedException(
            "OAuth2 support has been removed from Elsa.Studio.Authentication.ElsaAuth. " +
            "For OIDC use Elsa.Studio.Authentication.OpenIdConnect.*. " +
            "For legacy OAuth2 (ROPC) keep using the obsolete Elsa.Studio.Login implementation." );
    }

    /// <summary>
    /// Configures the login module to use OpenIdConnect (OIDC).
    /// </summary>
    [Obsolete("Elsa.Studio.Login OIDC integration is obsolete. Use Elsa.Studio.Authentication.OpenIdConnect.BlazorServer or Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.")]
    public static IServiceCollection UseOpenIdConnectCore(this IServiceCollection services, Action<Elsa.Studio.Login.Models.OpenIdConnectConfiguration> configure)
    {
        _ = configure;

        throw new NotSupportedException(
            "The legacy in-app OIDC code-flow is no longer available via Elsa.Studio.Authentication.ElsaAuth. " +
            "Migrate to the new modules: Elsa.Studio.Authentication.OpenIdConnect.BlazorServer (server) or " +
            "Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm (WASM)." );
    }
}