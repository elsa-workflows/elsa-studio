namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Defines clipboard-related operations that can be invoked from the application.
/// </summary>
public interface IClipboard
{
    /// <summary>
    /// Copies the specified text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that completes when the text has been copied.</returns>
    Task CopyText(string text, CancellationToken cancellationToken = default);
}