using Elsa.Api.Client.Activities;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// A service for managing diagram editors.
/// </summary>
public interface IDiagramEditorService
{
    /// <summary>
    /// Gets the diagram editor for the specified activity.
    /// </summary>
    IDiagramEditor GetDiagramEditor(Activity activity);
}