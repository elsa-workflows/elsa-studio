using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.Services;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Visualizers;
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
            .AddScoped<IUIFieldExtensionService, DefaultUIFieldExtensionService>()
            .AddScoped<IExpressionService, DefaultExpressionService>()
            .AddScoped<IStartupTaskRunner, DefaultStartupTaskRunner>()
            .AddScoped<IServerInformationProvider, EmptyServerInformationProvider>()
            .AddScoped<IClientInformationProvider, AssemblyClientInformationProvider>()
            .AddScoped<IWidgetRegistry, DefaultWidgetRegistry>()
            .AddScoped<IWidgetRegistry, DefaultWidgetRegistry>()
            .AddSingleton<IContentVisualizerProvider, DefaultContentVisualizerProvider>()
            .AddUserMessageService<DefaultUserMessageService>()
            ;

        // Content visualizers
        services.AddContentVisualizer<JsonContentVisualizer>();
        
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

    /// <summary>
    /// Adds the specified <see cref="IUIFieldExtensionHandler"/>.
    /// </summary>
    public static IServiceCollection AddUIFieldEnhancerHandler<T>(this IServiceCollection services) where T : class, IUIFieldExtensionHandler
    {
        return services.AddScoped<IUIFieldExtensionHandler, T>();
    }

    /// <summary>
    /// Adds the specified <see cref="IUserMessageService"/>.
    /// </summary>
    public static IServiceCollection AddUserMessageService<T>(this IServiceCollection services) where T : class, IUserMessageService
    {
        return services.AddScoped<IUserMessageService, T>();
    }

    /// <summary>
    /// Adds the specified <see cref="IContentVisualizer"/>.
    /// </summary>
    public static IServiceCollection AddContentVisualizer<T>(this IServiceCollection services) where T : class, IContentVisualizer
    {
        return services.AddTransient<IContentVisualizer, T>();
    }
}