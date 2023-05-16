using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Extensions;

public static class Core
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMenuService, DefaultMenuService>()
            .AddSingleton<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddSingleton<IThemeService, DefaultThemeService>()
            .AddSingleton<IAppBarService, DefaultAppBarService>()
            .AddSingleton<IModuleService, DefaultModuleService>()
            ;
    }
}