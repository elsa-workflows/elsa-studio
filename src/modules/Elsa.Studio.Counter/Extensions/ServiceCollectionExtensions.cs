using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Counter.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCounterModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, CounterMenu>();
    }
}