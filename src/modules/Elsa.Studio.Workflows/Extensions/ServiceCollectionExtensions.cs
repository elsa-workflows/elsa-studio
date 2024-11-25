using Elsa.Studio.ActivityPortProviders.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.UIHints.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.DiagramDesigners.Fallback;
using Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;
using Elsa.Studio.Workflows.Handlers;
using Elsa.Studio.Workflows.Menu;
using Elsa.Studio.Workflows.Services;
using Elsa.Studio.Workflows.Widgets;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;


namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflows module.
    /// </summary>
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WorkflowsMenu>()
            .AddScoped<IWorkflowInstanceObserverFactory, WorkflowInstanceObserverFactory>()
            .AddDefaultUIHintHandlers()
            .AddDefaultActivityPortProviders()
            .AddWorkflowsCore()
            .AddWorkflowsDesigner()
            .AddDomInterop()
            .AddClipboardInterop()
            .AddDownloadInterop()
            ;

        services
            .AddDiagramDesignerProvider<FallbackDesignerProvider>()
            .AddDiagramDesignerProvider<FlowchartDiagramDesignerProvider>();

        services.AddNotificationHandler<RefreshActivityRegistry>();
        services.AddScoped<IWidget, WorkflowDefinitionMetadataWidget>();
        services.AddScoped<IWidget, WorkflowDefinitionSettingsWidget>();
        services.AddScoped<IWidget, WorkflowDefinitionInfoWidget>();

        return services;
    }
}