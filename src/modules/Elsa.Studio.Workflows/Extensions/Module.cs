using Elsa.Studio.Contracts;
using Elsa.Studio.Designer.Extensions;
using Elsa.Studio.Workflows.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

public static class Module
{
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMenuProvider, WorkflowsMenu>()
            .AddDesigner();
    }
}