using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

/// <summary>
/// Extension methods for configuring OpenID Connect authentication in Blazor Server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenID Connect authentication services for Blazor Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for OIDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOidcAuthentication(
        this IServiceCollection services,
        Action<OidcOptions> configure)
    {
        var options = new OidcOptions();
        configure(options);

        // Ensure we always request the minimal identity scopes.
        var configuredScopes = options.AuthenticationScopes?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
        if (configuredScopes.Length == 0)
            configuredScopes = ["openid", "profile", "offline_access"];
        options.AuthenticationScopes = configuredScopes;

        // Set Blazor Server defaults for callback paths if not explicitly specified.
        options.CallbackPath ??= "/signin-oidc";
        options.SignedOutCallbackPath ??= "/signout-callback-oidc";

        // Register the token accessor and cache
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton(options);
        services.AddSingleton<IScopedTokenCache, MemoryScopedTokenCache>();
        services.AddScoped<IOidcTokenAccessor, ServerOidcTokenAccessor>();
        services.AddScoped<IHttpConnectionOptionsConfigurator, OpenIdConnect.Services.OidcHttpConnectionOptionsConfigurator>();
        services.AddScoped<IOidcRefreshConfigurationProvider, DefaultOidcRefreshConfigurationProvider>();
        services.AddScoped<OidcCookieTokenRefresher>();
        services.AddScoped<BrowserRefreshPingService>();
        services.AddOptions<OidcPersistedRefreshClientOptions>();
        services.AddScoped<OidcTokenRefreshOptionsAccessor>();

        // Configure ASP.NET Core authentication with cookie and OIDC
        services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookieOptions =>
            {
                cookieOptions.Cookie.Name = "ElsaStudio.Auth";
                cookieOptions.Cookie.HttpOnly = true;
                cookieOptions.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                cookieOptions.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                cookieOptions.ExpireTimeSpan = TimeSpan.FromHours(8);
                cookieOptions.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions =>
            {
                oidcOptions.Authority = options.Authority;
                oidcOptions.ClientId = options.ClientId;
                oidcOptions.ClientSecret = options.ClientSecret;
                oidcOptions.ResponseType = options.ResponseType;
                oidcOptions.UsePkce = options.UsePkce;
                oidcOptions.SaveTokens = options.SaveTokens;
                oidcOptions.CallbackPath = options.CallbackPath;
                oidcOptions.SignedOutCallbackPath = options.SignedOutCallbackPath;
                oidcOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                oidcOptions.GetClaimsFromUserInfoEndpoint = options.GetClaimsFromUserInfoEndpoint;

                // Configure scopes
                oidcOptions.Scope.Clear();
                foreach (var scope in options.AuthenticationScopes)
                {
                    oidcOptions.Scope.Add(scope);
                }

                // Map token response properties to enable token refresh
                oidcOptions.MapInboundClaims = false;
                
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    oidcOptions.MetadataAddress = options.MetadataAddress;
                }

                // Configure token validation parameters
                oidcOptions.TokenValidationParameters = new()
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ValidateIssuer = true
                };
            });

        // Add authorization services
        services.AddAuthorizationCore();

        // Use an OIDC-aware unauthorized component that initiates a challenge.
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<ChallengeToLogin>>();

        // Shared auth infrastructure (e.g. delegating handlers).
        services.AddAuthenticationInfrastructure();

        services.AddOptions<OidcTokenRefreshOptions>();
        services.AddHttpClient(OidcCookieTokenRefresher.AnonymousClientName);

        return services;
    }
}
