using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Counter.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Counter.Extensions;

public static class Module
{
    public static IServiceCollection AddCounterModule(this IServiceCollection services)
    {
        return services.AddSingleton<IMenuProvider, CounterMenu>();
    }
}