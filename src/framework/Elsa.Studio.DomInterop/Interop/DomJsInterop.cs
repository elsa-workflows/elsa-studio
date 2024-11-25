using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides access to the dom JavaScript module.
/// </summary>
public class DomJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IDomAccessor
{
    protected override string ModuleName => "dom";
    
    public async Task<DomRect> GetBoundingClientRectAsync(ElementRef element, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, element.Match()));
    public async Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<double>("getVisibleHeight", cancellationToken, elementRef.Match()));
    public async Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeVoidAsync("clickElement", cancellationToken, elementRef.Match()));
}