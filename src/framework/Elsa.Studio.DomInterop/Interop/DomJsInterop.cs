using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides access to the dom JavaScript module.
/// </summary>
public class DomJsInterop : JsInteropBase, IDomAccessor
{
    public DomJsInterop(IJSRuntime jsRuntime) : base(jsRuntime)
    {
    }

    protected override string ModuleName => "dom";
    
    public async Task<DomRect> GetBoundingClientRectAsync(ElementRef element, CancellationToken cancellationToken = default)
    {
        return await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, element.Match()));
    }

    public async Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default)
    {     
        return await InvokeAsync(async module => await module.InvokeAsync<double>("getVisibleHeight", cancellationToken, elementRef.Match()));
    }

    // public async Task<DomRect> GetBoundingClientRectAsync(Union<string, ElementReference> element, CancellationToken cancellationToken = default)
    // {
    //     return await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, element.Match<object>(value => value, value => value)));
    // }

    // public async Task<DomRect> GetBoundingClientRectAsync(ElementReference elementRef, CancellationToken cancellationToken = default)
    // {
    //     return await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, elementRef));
    // }
}