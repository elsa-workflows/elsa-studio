using Elsa.Studio.Contracts;
using Elsa.Studio.Webhooks.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Webhooks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebhooksModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WebhooksMenu>();
    }
}