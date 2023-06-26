using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.DomInterop.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomInterop(this IServiceCollection services)
    {
        services.TryAddScoped<DomJsInterop>();
        services.TryAddScoped<IDomAccessor, DomJsInterop>();
        
        return services;
    }
}