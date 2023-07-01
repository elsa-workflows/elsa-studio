namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Provides access to the browser's DOM.
/// </summary>
public interface IClipboard
{
    Task CopyText(string text, CancellationToken cancellationToken = default);
}