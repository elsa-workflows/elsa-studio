using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

public class FallbackDesignerProvider : IDiagramDesignerProvider
{
    public double Priority => -1000;
    public bool GetSupportsActivity(Activity activity) => true;

    public IDiagramDesigner GetEditor() => new FallbackDiagramDesigner();
}