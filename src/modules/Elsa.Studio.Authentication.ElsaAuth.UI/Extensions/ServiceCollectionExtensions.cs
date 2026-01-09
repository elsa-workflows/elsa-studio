using Elsa.Studio.Authentication.ElsaAuth.UI.ComponentProviders;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaAuth.UI.Extensions;

/// <summary>
/// Service registration extensions for the ElsaAuth UI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Elsa Identity login UI (route: <c>/login</c>) and an unauthorized redirect behavior.
    /// </summary>
    public static IServiceCollection AddElsaAuthUI(this IServiceCollection services)
    {
        // Provide a default unauthorized UI for Elsa Identity.
        services.AddScoped<IUnauthorizedComponentProvider, RedirectToLoginUnauthorizedComponentProvider>();

        // Optional shell feature (adds app-bar login state UI).
        services.AddScoped<IFeature, LoginFeature>();

        return services;
    }
}
