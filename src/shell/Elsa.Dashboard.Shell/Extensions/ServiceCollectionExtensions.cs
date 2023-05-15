using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Shell.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Elsa.Dashboard.Shell.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShell(this IServiceCollection services)
    {
        return services
            .AddMudServices()
            .AddDashboardServices()
            .AddSingleton<IStartupTask, InitializeModulesStartupTask>();
    }
}