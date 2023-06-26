using Elsa.Studio.Extensions;
using Elsa.Studio.UIHintHandlers.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.UIHintHandlers.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultUIHintHandlers(this IServiceCollection services)
    {
        return services
            .AddUIHintHandler<SingleLineHandler>()
            .AddUIHintHandler<CheckboxHandler>()
            .AddUIHintHandler<CheckListHandler>()
            .AddUIHintHandler<MultiTextHandler>()
            .AddUIHintHandler<MultiLineHandler>()
            ;
    }
}