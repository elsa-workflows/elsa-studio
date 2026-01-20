using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;

/// <summary>
/// Service registration extensions for the ElsaIdentity UI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Elsa Identity login UI (route: <c>/login</c>) and an unauthorized redirect behavior.
    /// </summary>
    public static IServiceCollection AddElsaIdentityUI(this IServiceCollection services)
    {
        services.AddScoped<IFeature, ElsaIdentityUIFeature>();
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<RedirectToLogin>>();

        return services;
    }
}
