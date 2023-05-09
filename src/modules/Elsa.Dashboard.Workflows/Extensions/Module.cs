using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Workflows.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Workflows.Extensions;

public static class Module
{
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        return services.AddSingleton<IMenuProvider, WorkflowsMenu>();
    }
}