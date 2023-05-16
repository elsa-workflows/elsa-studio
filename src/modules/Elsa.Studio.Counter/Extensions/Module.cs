using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Counter.Extensions;

public static class Module
{
    public static IServiceCollection AddCounterModule(this IServiceCollection services)
    {
        return services.AddSingleton<IMenuProvider, CounterMenu>();
    }
}