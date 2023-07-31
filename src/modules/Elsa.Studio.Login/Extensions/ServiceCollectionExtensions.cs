using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the login module to the service collection.
    /// </summary>
    public static IServiceCollection AddLoginModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IModule, Module>()
            .AddScoped<ILoginPageProvider, LoginPageProvider>()
            .AddScoped<ICredentialsValidator, DefaultCredentialsValidator>()
            ;
    }
}