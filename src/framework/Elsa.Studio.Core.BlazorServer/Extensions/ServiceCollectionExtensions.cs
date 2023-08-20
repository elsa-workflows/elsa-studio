using Elsa.Studio.Core.BlazorServer.HostedServices;
using Elsa.Studio.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Core.BlazorServer.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services with Blazor Server implementations.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        // Register core.
        services.AddCoreInternal();
        
        // Register hosted services.
        services.AddHostedService<RunStartupTasksHostedService>();
        
        return services;
    }
}