using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Core.Contracts;

namespace Elsa.Studio.Workflows.Core.Services;

public class DefaultDiagramEditorService : IDiagramEditorService
{
    private readonly IEnumerable<IDiagramEditorProvider> _providers;

    public DefaultDiagramEditorService(IEnumerable<IDiagramEditorProvider> providers)
    {
        _providers = providers;
    }
    
    public IDiagramEditor GetDiagramEditor(Activity activity)
    {
        var provider = _providers.FirstOrDefault(x => x.GetSupportsActivity(activity)) ?? throw new Exception($"No diagram editor provider found for activity {activity.Type}.");
        return provider.GetEditor();
    }
}