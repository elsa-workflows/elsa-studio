using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Providers;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Providers;
using Elsa.Studio.Workflows.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
    {
        services
            .AddSingleton<IFeatureService, RemoteFeatureService>()
            .AddSingleton<IWorkflowDefinitionService, RemoteWorkflowDefinitionService>()
            .AddSingleton<IWorkflowInstanceService, RemoteWorkflowInstanceService>()
            .AddSingleton<IActivityRegistryProvider, RemoteActivityRegistryProvider>()
            .AddSingleton<IActivityExecutionService, RemoteActivityExecutionService>()
            .AddSingleton<IActivityRegistry, DefaultActivityRegistry>()
            .AddSingleton<IStorageDriverService, RemoteStorageDriverService>()
            .AddSingleton<IVariableTypeService, RemoteVariableTypeService>()
            .AddSingleton<IWorkflowActivationStrategyService, RemoteWorkflowActivationStrategyService>()
            .AddSingleton<IDiagramDesignerService, DefaultDiagramDesignerService>()
            .AddSingleton<IActivityDisplaySettingsRegistry, DefaultActivityDisplaySettingsRegistry>()
            .AddSingleton<IActivityPortService, DefaultActivityPortService>()
            .AddSingleton<IActivityVisitor, DefaultActivityVisitor>()
            ;

        services.AddActivityDisplaySettingsProvider<DefaultActivityDisplaySettingsProvider>();
        services.AddActivityPortProvider<DefaultActivityPortProvider>();
        
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