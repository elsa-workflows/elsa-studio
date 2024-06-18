using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Implement this interface to provide a diagram editor for a given workflow definition's root activity.
/// </summary>
public interface IDiagramDesignerProvider
{
    double Priority { get; }
    bool GetSupportsActivity(JsonObject activity, WorkflowSubgraph workflowSubgraph);
    IDiagramDesigner GetEditor();
}