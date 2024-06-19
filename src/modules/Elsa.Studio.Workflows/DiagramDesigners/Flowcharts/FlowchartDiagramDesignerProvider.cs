using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer provider for the Flowchart designer.
/// </summary>
[UsedImplicitly]
public class FlowchartDiagramDesignerProvider : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.Flowchart";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new FlowchartDiagramDesigner();
}