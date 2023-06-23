using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Core.Contracts;

namespace Elsa.Studio.Workflows.DiagramEditors.Flowcharts;

public class FlowchartDiagramEditorProvider : IDiagramEditorProvider
{
    public bool GetSupportsActivity(Activity activity) => activity is Flowchart;

    public IDiagramEditor GetEditor()
    {
        return new FlowchartDiagramEditor();
    }
}