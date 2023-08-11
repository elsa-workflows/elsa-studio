using Elsa.Studio.Extensions;
using Elsa.Studio.UIHintHandlers.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.UIHintHandlers.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default UI hint handlers.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultUIHintHandlers(this IServiceCollection services)
    {
        return services
            .AddUIHintHandler<SingleLineHandler>()
            .AddUIHintHandler<CheckboxHandler>()
            .AddUIHintHandler<CheckListHandler>()
            .AddUIHintHandler<MultiTextHandler>()
            .AddUIHintHandler<MultiLineHandler>()
            .AddUIHintHandler<DropdownHandler>()
            .AddUIHintHandler<CodeEditorHandler>()
            .AddUIHintHandler<SwitchEditorHandler>()
            .AddUIHintHandler<HttpStatusCodesHandler>()
            .AddUIHintHandler<VariablePickerHandler>()
            .AddUIHintHandler<WorkflowDefinitionPickerHandler>()
            .AddUIHintHandler<OutputPickerHandler>()
            .AddUIHintHandler<OutcomePickerHandler>()
            ;
    }
}