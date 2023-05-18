using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Secrets.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretsModule(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddSingleton<IMenuProvider, SecretsMenu>();
    }
}