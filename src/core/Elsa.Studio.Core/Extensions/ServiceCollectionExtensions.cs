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
            .AddScoped<IMenuService, DefaultMenuService>()
            .AddScoped<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddScoped<IThemeService, DefaultThemeService>()
            .AddScoped<IAppBarService, DefaultAppBarService>()
            .AddScoped<IModuleService, DefaultModuleService>()
            .AddScoped<IUIHintService, DefaultUIHintService>()
            .AddScoped<ISyntaxService, DefaultSyntaxService>()
            .AddScoped<IActivityIdGenerator, ShortGuidActivityIdGenerator>()
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
    
    public static IServiceCollection AddUIHintHandler<T>(this IServiceCollection services) where T : class, IUIHintHandler
    {
        return services.AddScoped<IUIHintHandler, T>();
    }
    
    public static IServiceCollection AddSyntaxProvider<T>(this IServiceCollection services) where T : class, ISyntaxProvider
    {
        return services.AddScoped<ISyntaxProvider, T>();
    }
}