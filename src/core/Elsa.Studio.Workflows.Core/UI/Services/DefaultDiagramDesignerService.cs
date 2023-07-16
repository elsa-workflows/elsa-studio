using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.UI.Services;

public class DefaultDiagramDesignerService : IDiagramDesignerService
{
    private readonly IEnumerable<IDiagramDesignerProvider> _providers;

    public DefaultDiagramDesignerService(IEnumerable<IDiagramDesignerProvider> providers)
    {
        _providers = providers;
    }
    
    public IDiagramDesigner GetDiagramDesigner(JsonObject activity)
    {
        var provider = _providers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault(x => x.GetSupportsActivity(activity)) ?? throw new Exception($"No diagram editor provider found for activity {activity.GetTypeName()}.");
        return provider.GetEditor();
    }
}