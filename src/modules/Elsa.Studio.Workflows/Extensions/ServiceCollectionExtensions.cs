using Elsa.Studio.Contracts;
using Elsa.Studio.Designer.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Menu;
using Elsa.Studio.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddSingleton<IMenuProvider, WorkflowsMenu>()
            .AddSingleton<IWorkflowDefinitionService, DefaultWorkflowDefinitionService>()
            .AddDesigner();
    }
}