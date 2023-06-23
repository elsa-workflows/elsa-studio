using Elsa.Api.Client.Activities;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Implement this interface to provide a diagram editor for a given workflow definition's root activity.
/// </summary>
public interface IDiagramEditorProvider
{
    bool GetSupportsActivity(Activity activity);
    IDiagramEditor GetEditor();
}