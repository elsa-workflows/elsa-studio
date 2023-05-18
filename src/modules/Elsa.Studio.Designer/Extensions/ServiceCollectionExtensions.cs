using Elsa.Studio.Contracts;
using Elsa.Studio.Designer.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Designer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesigner(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddScoped<DesignerJsInterop>();
    }
}