using Elsa.Studio.Workflows.ActivityDisplaySettingsProviders;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.PortProviders;
using Elsa.Studio.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowDefinitionService, DefaultWorkflowDefinitionService>()
            .AddSingleton<IActivityRegistry, RemoteActivityRegistry>()
            .AddSingleton<IStorageDriverService, RemoteStorageDriverService>()
            .AddSingleton<IVariableTypeService, RemoteVariableTypeService>()
            .AddSingleton<IWorkflowActivationStrategyService, RemoteWorkflowActivationStrategyService>()
            .AddSingleton<IDiagramDesignerService, DefaultDiagramDesignerService>()
            .AddSingleton<IActivityDisplaySettingsRegistry, DefaultActivityDisplaySettingsRegistry>()
            .AddSingleton<IActivityPortService, DefaultActivityPortService>()
            ;

        services.AddActivityDisplaySettingsProvider<DefaultActivityDisplaySettingsProvider>();
        services.AddActivityPortProvider<DefaultActivityPortProvider>();
        services.AddActivityPortProvider<FlowSwitchPortProvider>();
        
        return services;
    }
    
    public static IServiceCollection AddDiagramDesignerProvider<T>(this IServiceCollection services) where T : class, IDiagramDesignerProvider
    {
        services.AddSingleton<IDiagramDesignerProvider, T>();
        return services;
    }
    
    public static IServiceCollection AddActivityDisplaySettingsProvider<T>(this IServiceCollection services) where T : class, IActivityDisplaySettingsProvider
    {
        services.AddSingleton<IActivityDisplaySettingsProvider, T>();
        return services;
    }
    
    public static IServiceCollection AddActivityPortProvider<T>(this IServiceCollection services) where T : class, IActivityPortProvider
    {
        services.AddSingleton<IActivityPortProvider, T>();
        return services;
    }
}