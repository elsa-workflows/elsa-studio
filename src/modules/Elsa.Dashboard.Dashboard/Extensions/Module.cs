using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Dashboard.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Dashboard.Extensions;

public static class Module
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services.AddSingleton<IMenuProvider, DashboardMenu>();
    }
}