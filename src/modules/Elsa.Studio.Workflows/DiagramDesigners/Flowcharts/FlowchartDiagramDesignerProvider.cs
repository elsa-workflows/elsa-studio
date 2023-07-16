using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

public class FlowchartDiagramDesignerProvider : IDiagramDesignerProvider
{
    public double Priority => 0;
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.Flowchart";

    public IDiagramDesigner GetEditor()
    {
        return new FlowchartDiagramDesigner();
    }
}