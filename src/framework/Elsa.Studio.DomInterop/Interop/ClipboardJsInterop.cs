using Elsa.Studio.DomInterop.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides access to the dom JavaScript module.
/// </summary>
public class ClipboardJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IClipboard
{
    protected override string ModuleName => "clipboard";

    public async Task CopyText(string text, CancellationToken cancellationToken = default)
    {
        await InvokeAsync(async module => await module.InvokeVoidAsync("copyText", cancellationToken, text));
    }
}