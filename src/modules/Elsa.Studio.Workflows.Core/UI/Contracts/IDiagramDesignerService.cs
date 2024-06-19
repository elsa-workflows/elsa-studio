using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// A service for managing diagram editors.
/// </summary>
public interface IDiagramDesignerService
{
    /// <summary>
    /// Gets the diagram designer for the specified activity.
    /// </summary>
    IDiagramDesigner GetDiagramDesigner(JsonObject activity);
}