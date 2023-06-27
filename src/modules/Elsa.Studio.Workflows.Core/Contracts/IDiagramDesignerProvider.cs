using Elsa.Api.Client.Activities;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Implement this interface to provide a diagram editor for a given workflow definition's root activity.
/// </summary>
public interface IDiagramDesignerProvider
{
    double Priority { get; }
    bool GetSupportsActivity(Activity activity);
    IDiagramDesigner GetEditor();
}