using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogin(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddSingleton<ILoginPageProvider, LoginPageProvider>();
    }
}