using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Contracts;
using Elsa.Studio.WorkflowContexts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.WorkflowContexts.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the login module to the service collection.
    /// </summary>
    public static IServiceCollection AddWorkflowContextsModule(this IServiceCollection services)
    {
        return services
                .AddScoped<IFeature, Feature>()
                .AddScoped<IWorkflowContextsProvider, RemoteWorkflowContextsProvider>()
            ;
    }
}