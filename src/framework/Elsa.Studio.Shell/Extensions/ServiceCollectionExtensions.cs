using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Shell.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Elsa.Studio.Shell.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShell(this IServiceCollection services)
    {
        return services
            .AddMudServices()
            .AddCore()
            .AddSingleton<IStartupTask, InitializeModulesStartupTask>();
    }
}