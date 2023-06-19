using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
    {
        return services
            .AddSingleton<IWorkflowDefinitionService, DefaultWorkflowDefinitionService>()
            .AddSingleton<IActivityRegistry, DefaultActivityRegistry>();
    }
}