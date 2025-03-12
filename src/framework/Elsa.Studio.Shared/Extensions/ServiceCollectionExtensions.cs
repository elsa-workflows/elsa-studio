using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Monaco.Handlers;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Adds shared services to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared services to the service collection.
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<TypeDefinitionService>();
        
        // TODO: Move this to a new package; either specific to JS, or perhaps a general Monaco package that supports multiple languages.
        // We'll decide once we add support for more languages.
        services.AddScoped<IMonacoHandler, JavaScriptMonacoHandler>();

        // Time services.
        services.AddScoped<ITimeFormatter, DefaultTimeFormatter>();
        services.AddScoped<ITimeZoneProvider, UtcTimeZoneProvider>();
        services.AddScoped<IBrandingProvider, DefaultBrandingProvider>();
        return services;
    }
}