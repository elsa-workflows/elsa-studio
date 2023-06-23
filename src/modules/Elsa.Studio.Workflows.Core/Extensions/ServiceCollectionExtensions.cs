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
            .AddSingleton<IDiagramEditorService, DefaultDiagramEditorService>()
            ;
        
        return services;
    }
    
    public static IServiceCollection AddDiagramEditorProvider<T>(this IServiceCollection services) where T : class, IDiagramEditorProvider
    {
        services.AddSingleton<IDiagramEditorProvider, T>();
        return services;
    }
}