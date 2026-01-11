using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Extensions;

/// <summary>
/// Service registrations for ElsaAuth in Blazor WebAssembly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ElsaAuth services with Blazor WebAssembly implementations.
    /// </summary>
    public static IServiceCollection AddElsaAuth(this IServiceCollection services)
    {
        services.AddElsaAuthCore();
        services.AddScoped<IJwtAccessor, BlazorWasmJwtAccessor>();

        return services;
    }
}

