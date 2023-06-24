using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.UIHintHandlers.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Core.Extensions;
using Elsa.Studio.Workflows.DiagramEditors.Flowcharts;
using Elsa.Studio.Workflows.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        services
            .AddSingleton<IModule, Module>()
            .AddSingleton<IMenuProvider, WorkflowsMenu>()
            .AddDefaultUIHintHandlers()
            .AddActivityTypeService()
            .AddWorkflowsCore()
            .AddWorkflowsDesigner();

        services.AddDiagramDesignerProvider<FlowchartDiagramDesignerProvider>();
        
        return services;
    }
}