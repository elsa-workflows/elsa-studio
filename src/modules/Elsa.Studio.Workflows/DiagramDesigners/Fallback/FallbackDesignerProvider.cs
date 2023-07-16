using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

public class FallbackDesignerProvider : IDiagramDesignerProvider
{
    public double Priority => -1000;
    public bool GetSupportsActivity(JsonObject activity) => true;

    public IDiagramDesigner GetEditor() => new FallbackDiagramDesigner();
}