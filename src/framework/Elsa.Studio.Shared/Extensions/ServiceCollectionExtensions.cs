using Elsa.Studio.Contracts;
using Elsa.Studio.MonacoHandlers;
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
        return services;
    }
}