using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.DomInterop.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IDomAccessor, DomJsInterop>();

        return services;
    }

    public static IServiceCollection AddClipboardInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IClipboard, ClipboardJsInterop>();

        return services;
    }
    
    public static IServiceCollection AddDownloadInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IDownload, DownloadJsInterop>();

        return services;
    }
}