namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Represents a workspace.
/// </summary>
public interface IWorkspace
{
    /// <summary>
    /// Gets a value indicating whether the workspace is read-only.
    /// </summary>
    bool IsReadOnly { get; }
}