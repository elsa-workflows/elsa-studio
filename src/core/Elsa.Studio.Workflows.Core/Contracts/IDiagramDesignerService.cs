using Elsa.Api.Client.Activities;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// A service for managing diagram editors.
/// </summary>
public interface IDiagramDesignerService
{
    /// <summary>
    /// Gets the diagram designer for the specified activity.
    /// </summary>
    IDiagramDesigner GetDiagramDesigner(Activity activity);
}