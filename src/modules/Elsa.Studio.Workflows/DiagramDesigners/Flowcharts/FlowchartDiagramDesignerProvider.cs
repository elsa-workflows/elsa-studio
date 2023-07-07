using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

public class FlowchartDiagramDesignerProvider : IDiagramDesignerProvider
{
    public double Priority => 0;
    public bool GetSupportsActivity(Activity activity) => activity is Flowchart;

    public IDiagramDesigner GetEditor()
    {
        return new FlowchartDiagramDesigner();
    }
}