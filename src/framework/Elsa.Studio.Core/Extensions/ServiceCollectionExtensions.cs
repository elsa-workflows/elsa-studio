using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Options;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core services.
    /// </summary>
    public static IServiceCollection AddCoreInternal(this IServiceCollection services)
    {
        services
            .AddScoped<IBlazorServiceAccessor, BlazorServiceAccessor>()
            .AddScoped<IMenuService, DefaultMenuService>()
            .AddScoped<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddScoped<IThemeService, DefaultThemeService>()
            .AddScoped<IAppBarService, DefaultAppBarService>()
            .AddScoped<IFeatureService, DefaultFeatureService>()
            .AddScoped<IUIHintService, DefaultUIHintService>()
            .AddScoped<IExpressionService, DefaultExpressionService>()
            .AddScoped<IStartupTaskRunner, DefaultStartupTaskRunner>()
            .AddScoped<IServerInformationProvider, EmptyServerInformationProvider>()
            .AddScoped<IClientInformationProvider, AssemblyClientInformationProvider>()
            .AddScoped<IWidgetRegistry, DefaultWidgetRegistry>()
            ;
        
        // Mediator.
        services.AddScoped<IMediator, DefaultMediator>();

        //Localization
        services.AddSingleton<ILocalizationProvider, DefaultLocalizationProvider>();
        services.AddSingleton<ILocalizer, DefaultLocalizer>();
        return services;
    }
    
    /// <summary>
    /// Adds backend services to the service collection.
    /// </summary>
    public static IServiceCollection AddRemoteBackend(this IServiceCollection services, BackendApiConfig? config = null)
    {
        services.Configure(config?.ConfigureBackendOptions ?? (_ => { }));
        services.AddDefaultApiClients(config?.ConfigureHttpClientBuilder);
        services.TryAddScoped<IRemoteBackendAccessor, DefaultRemoteBackendAccessor>();
        services.TryAddScoped<IBackendApiClientProvider, DefaultBackendApiClientProvider>();
        return services;
    }
    
    public static IServiceCollection AddRemoteApi<TApi>(this IServiceCollection services, BackendApiConfig? config = null) where TApi : class
    {
        services.Configure(config?.ConfigureBackendOptions ?? (_ => { }));
        services.AddApiClient<TApi>(config?.ConfigureHttpClientBuilder);
        return services;
    }

    /// <summary>
    /// Adds the specified <see cref="INotificationHandler"/>.
    /// </summary>
    public static IServiceCollection AddNotificationHandler<T>(this IServiceCollection services) where T: class, INotificationHandler
    {
        return services.AddScoped<INotificationHandler, T>();
    }
    
    /// <summary>
    /// Adds the specified <see cref="IUIHintHandler"/>.
    /// </summary>
    public static IServiceCollection AddUIHintHandler<T>(this IServiceCollection services) where T : class, IUIHintHandler
    {
        return services.AddScoped<IUIHintHandler, T>();
    }
}