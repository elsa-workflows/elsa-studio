using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Authentication.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationUI(this IServiceCollection services)
    {
        services.TryAddScoped<ILoginMethodComponentRegistry, LoginMethodComponentRegistry>();
        services.TryAddScoped<ILoginMethodIconRegistry, LoginMethodIconRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IFeature, Feature>());
        return services;
    }
}
