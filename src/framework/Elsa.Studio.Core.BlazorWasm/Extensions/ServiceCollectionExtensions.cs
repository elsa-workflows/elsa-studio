using Elsa.Studio.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Core.BlazorWasm.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services with WASM implementations.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        // Register core.
        services.AddCoreInternal();
        
        return services;
    }
}