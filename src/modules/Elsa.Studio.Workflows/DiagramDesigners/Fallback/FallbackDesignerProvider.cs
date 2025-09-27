using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

/// <summary>
/// Provides fallback designer services.
/// </summary>
public class FallbackDesignerProvider : IDiagramDesignerProvider
{
    /// <summary>
    /// Provides the priority.
    /// </summary>
    public double Priority => -1000;
    /// <summary>
    /// Provides the get supports activity.
    /// </summary>
    public bool GetSupportsActivity(JsonObject activity) => true;

    /// <summary>
    /// Provides the get editor.
    /// </summary>
    public IDiagramDesigner GetEditor() => new FallbackDiagramDesigner();
}