using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Core.BlazorWasm.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core services with WASM implementations for services like <see cref="IJwtAccessor"/> amd <see cref="IJwtParser"/>.
    /// </summary>
    public static IServiceCollection AddBlazorWasmModule(this IServiceCollection services)
    {
        services.AddSingleton<IJwtParser, ClientJwtParser>();
        services.AddScoped<IJwtAccessor, ClientJwtAccessor>();
        
        return services;
    }
}