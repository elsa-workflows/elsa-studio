using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.ElsaAuth.UI.Components;
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
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<RedirectToLogin>>();
        services.AddScoped<IFeature, LoginFeature>();

        return services;
    }
}
