using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUIHintHandler<T>(this IServiceCollection services) where T : class, IUIHintHandler
    {
        return services.AddSingleton<IUIHintHandler, T>();
    }
}