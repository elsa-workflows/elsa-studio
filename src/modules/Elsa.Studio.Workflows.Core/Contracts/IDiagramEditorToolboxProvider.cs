using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Implement this interface to provide toolbox items for the diagram editor.
/// </summary>
public interface IDiagramEditorToolboxProvider : IDiagramEditor
{
    IEnumerable<RenderFragment> GetToolboxItems();
}