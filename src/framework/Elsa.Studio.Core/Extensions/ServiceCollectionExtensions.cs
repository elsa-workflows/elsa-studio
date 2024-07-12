using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Options;
using Elsa.Studio.Contracts;
using Elsa.Studio.Options;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

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
        
        return services;
    }
    
    /// <summary>
    /// Adds backend services to the service collection.
    /// </summary>
    public static IServiceCollection AddRemoteBackend(this IServiceCollection services, Action<ElsaClientBuilderOptions> configureElsaClient, Action<BackendOptions>? configureBackendOptions = default)
    {
        services.Configure(configureBackendOptions ?? (_ => { }));
        services.AddDefaultApiClients(configureElsaClient);
        
        return services
                .AddScoped<IRemoteBackendAccessor, DefaultRemoteBackendAccessor>()
                .AddScoped<IRemoteBackendApiClientProvider, DefaultRemoteBackendApiClientProvider>()
            ;
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