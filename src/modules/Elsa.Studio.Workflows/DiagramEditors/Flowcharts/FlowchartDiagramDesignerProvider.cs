using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Core.Contracts;

namespace Elsa.Studio.Workflows.DiagramEditors.Flowcharts;

public class FlowchartDiagramDesignerProvider : IDiagramDesignerProvider
{
    public bool GetSupportsActivity(Activity activity) => activity is Flowchart;

    public IDiagramDesigner GetEditor()
    {
        return new FlowchartDiagramDesigner();
    }
}