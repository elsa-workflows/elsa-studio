using Elsa.Studio.Extensions;
using Elsa.Studio.UIHints.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default UI hint handlers.
    /// </summary>
    public static IServiceCollection AddDefaultUIHintHandlers(this IServiceCollection services)
    {
        return services
            .AddUIHintHandler<SingleLineHandler>()
            .AddUIHintHandler<CheckboxHandler>()
            .AddUIHintHandler<CheckListHandler>()
            .AddUIHintHandler<MultiTextHandler>()
            .AddUIHintHandler<MultiLineHandler>()
            .AddUIHintHandler<DropDownHandler>()
            .AddUIHintHandler<CodeEditorHandler>()
            .AddUIHintHandler<JsonEditorHandler>()
            .AddUIHintHandler<SwitchEditorHandler>()
            .AddUIHintHandler<HttpStatusCodesHandler>()
            .AddUIHintHandler<VariablePickerHandler>()
            .AddUIHintHandler<WorkflowDefinitionPickerHandler>()
            .AddUIHintHandler<OutputPickerHandler>()
            .AddUIHintHandler<OutcomePickerHandler>()
            .AddUIHintHandler<DynamicOutcomesHandler>()
            ;
    }
}