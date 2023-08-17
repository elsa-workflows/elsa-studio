using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.HostedServices;
using Elsa.Studio.Core.BlazorServer.Services;
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
    /// Adds the core services with WASM implementations for services like <see cref="IJwtAccessor"/> amd <see cref="IJwtParser"/>.
    /// </summary>
    public static IServiceCollection AddBlazorServerModule(this IServiceCollection services)
    {
        // Register HttpContextAccessor.
        services.AddHttpContextAccessor();
        
        // Register JWT services.
        services.AddSingleton<IJwtParser, ServerJwtParser>();
        services.AddScoped<IJwtAccessor, ServerJwtAccessor>();
        
        // Register hosted services.
        services.AddHostedService<RunStartupTasksHostedService>();
        
        return services;
    }
}