using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Services;
using Elsa.Studio.Authentication.ElsaAuth.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Extensions;

/// <summary>
/// Service registrations for ElsaAuth in Blazor Server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ElsaAuth services with Blazor Server implementations.
    /// </summary>
    public static IServiceCollection AddElsaAuth(this IServiceCollection services)
    {
        services.AddElsaAuthCore();

        services.AddHttpContextAccessor();
        services.AddBlazoredLocalStorage();

        services.AddSingleton<IJwtParser, JwtParser>();
        services.AddScoped<IJwtAccessor, BlazorServerJwtAccessor>();

        return services;
    }
}

