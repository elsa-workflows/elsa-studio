using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Elsa.Studio.SyntaxProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Extensions;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services
            .AddSingleton<IMenuService, DefaultMenuService>()
            .AddSingleton<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddSingleton<IThemeService, DefaultThemeService>()
            .AddSingleton<IAppBarService, DefaultAppBarService>()
            .AddSingleton<IModuleService, DefaultModuleService>()
            .AddSingleton<IUIHintService, DefaultUIHintService>()
            .AddSingleton<ISyntaxService, DefaultSyntaxService>()
            .AddSingleton<IActivityIdGenerator, ShortGuidActivityIdGenerator>()
            ;

        // Syntax providers.
        services
            .AddSyntaxProvider<LiteralSyntaxProvider>()
            .AddSyntaxProvider<JavaScriptSyntaxProvider>()
            .AddSyntaxProvider<LiquidSyntaxProvider>()
            .AddSyntaxProvider<ObjectSyntaxProvider>()
            ;
        
        // Mediator.
        services.AddSingleton<IMediator, DefaultMediator>();
        
        return services;
    }
    
    public static IServiceCollection AddUIHintHandler<T>(this IServiceCollection services) where T : class, IUIHintHandler
    {
        return services.AddSingleton<IUIHintHandler, T>();
    }
    
    public static IServiceCollection AddSyntaxProvider<T>(this IServiceCollection services) where T : class, ISyntaxProvider
    {
        return services.AddSingleton<ISyntaxProvider, T>();
    }
}