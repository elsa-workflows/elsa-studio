using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddSingleton<IMenuProvider, DashboardMenu>();
    }
}