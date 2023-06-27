using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Core.Contracts;

namespace Elsa.Studio.Workflows.Core.Services;

public class DefaultDiagramDesignerService : IDiagramDesignerService
{
    private readonly IEnumerable<IDiagramDesignerProvider> _providers;

    public DefaultDiagramDesignerService(IEnumerable<IDiagramDesignerProvider> providers)
    {
        _providers = providers;
    }
    
    public IDiagramDesigner GetDiagramDesigner(Activity activity)
    {
        var provider = _providers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault(x => x.GetSupportsActivity(activity)) ?? throw new Exception($"No diagram editor provider found for activity {activity.Type}.");
        return provider.GetEditor();
    }
}