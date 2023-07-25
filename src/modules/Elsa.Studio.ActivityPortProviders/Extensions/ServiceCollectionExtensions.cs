using Elsa.Studio.ActivityPortProviders.Providers;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.ActivityPortProviders.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultActivityPortProviders(this IServiceCollection services)
    {
        services.AddActivityPortProvider<SwitchPortProvider>();
        services.AddActivityPortProvider<FlowSwitchPortProvider>();
        services.AddActivityPortProvider<SendHttpRequestPortProvider>();
        
        return services;
    }
}