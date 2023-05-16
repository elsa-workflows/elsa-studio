using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Dashboard.Extensions;

public static class Module
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services.AddSingleton<IMenuProvider, DashboardMenu>();
    }
}