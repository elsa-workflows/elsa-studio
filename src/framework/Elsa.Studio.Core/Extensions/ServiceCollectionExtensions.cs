using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Elsa.Studio.SyntaxProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core services.
    /// </summary>
    public static IServiceCollection AddCoreInternal(this IServiceCollection services)
    {
        services
            .AddScoped<IMenuService, DefaultMenuService>()
            .AddScoped<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddScoped<IThemeService, DefaultThemeService>()
            .AddScoped<IAppBarService, DefaultAppBarService>()
            .AddScoped<IModuleService, DefaultModuleService>()
            .AddScoped<IUIHintService, DefaultUIHintService>()
            .AddScoped<ISyntaxService, DefaultSyntaxService>()
            .AddScoped<IStartupTaskRunner, DefaultStartupTaskRunner>()
            .AddScoped<IServerInformationProvider, EmptyServerInformationProvider>()
            ;

        // Syntax providers.
        services
            .AddSyntaxProvider<LiteralSyntaxProvider>()
            .AddSyntaxProvider<JavaScriptSyntaxProvider>()
            .AddSyntaxProvider<LiquidSyntaxProvider>()
            .AddSyntaxProvider<ObjectSyntaxProvider>()
            ;
        
        // Mediator.
        services.AddScoped<IMediator, DefaultMediator>();
        
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
    /// Ads the specified <see cref="ISyntaxProvider"/>.
    /// </summary>
    public static IServiceCollection AddSyntaxProvider<T>(this IServiceCollection services) where T : class, ISyntaxProvider
    {
        return services.AddScoped<ISyntaxProvider, T>();
    }
}