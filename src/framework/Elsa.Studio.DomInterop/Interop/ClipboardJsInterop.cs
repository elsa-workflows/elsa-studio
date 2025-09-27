using Elsa.Studio.DomInterop.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides clipboard helper operations implemented in JavaScript.
/// </summary>
public class ClipboardJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IClipboard
{
    /// <inheritdoc />
    protected override string ModuleName => "clipboard";

    /// <summary>
    /// Copies the specified text to the clipboard using the browser's clipboard API.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="cancellationToken">A token to cancel the copy request.</param>
    /// <returns>A task that completes when the text has been copied.</returns>
    public async Task CopyText(string text, CancellationToken cancellationToken = default)
    {
        await InvokeAsync(async module => await module.InvokeVoidAsync("copyText", cancellationToken, text));
    }
}