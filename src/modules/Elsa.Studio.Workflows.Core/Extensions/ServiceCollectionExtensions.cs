using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowDefinitionService, DefaultWorkflowDefinitionService>()
            .AddSingleton<IActivityRegistry, DefaultActivityRegistry>()
            .AddSingleton<IDiagramDesignerService, DefaultDiagramDesignerService>()
            .AddSingleton<IActivityDisplaySettingsRegistry, DefaultActivityDisplaySettingsRegistry>()
            ;
        
        return services;
    }
    
    public static IServiceCollection AddDiagramDesignerProvider<T>(this IServiceCollection services) where T : class, IDiagramDesignerProvider
    {
        services.AddSingleton<IDiagramDesignerProvider, T>();
        return services;
    }
}