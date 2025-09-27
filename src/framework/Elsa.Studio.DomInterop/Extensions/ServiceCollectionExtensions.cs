using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.DomInterop.Extensions;

/// <summary>
/// Provides extension methods for registering DOM interop services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers services that facilitate DOM interactions via JavaScript interop.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddDomInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IDomAccessor, DomJsInterop>();

        return services;
    }

    /// <summary>
    /// Registers services that expose clipboard functionality via JavaScript interop.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddClipboardInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IClipboard, ClipboardJsInterop>();

        return services;
    }

    /// <summary>
    /// Registers services that allow downloading files from streams using JavaScript interop.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddDownloadInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IFiles, FilesJsInterop>();

        return services;
    }
}