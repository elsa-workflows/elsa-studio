using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowDesigner.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.WorkflowDesigner.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesigner(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddScoped<DesignerJsInterop>();
    }
}