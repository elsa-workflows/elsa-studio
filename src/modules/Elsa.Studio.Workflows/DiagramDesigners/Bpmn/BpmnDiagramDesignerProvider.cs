using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A diagram designer provider for an imported <c>Elsa.BpmnProcess</c> root, so it opens in the BPMN
/// designer instead of falling back to the JSON view.
/// </summary>
[UsedImplicitly]
public class BpmnDiagramDesignerProvider(ILocalizer localizer, IOptions<DesignerOptions> designerOptions) : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 10;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.BpmnProcess";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new BpmnDiagramDesigner(localizer, designerOptions);
}
