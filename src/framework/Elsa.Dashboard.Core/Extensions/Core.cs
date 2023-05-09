using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Extensions;

public static class Core
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMenuService, DefaultMenuService>()
            .AddSingleton<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddSingleton<IThemeService, DefaultThemeService>();
    }
}