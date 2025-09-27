using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides DOM convenience operations implemented via JavaScript interop.
/// </summary>
public class DomJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IDomAccessor
{
    /// <inheritdoc />
    protected override string ModuleName => "dom";

    /// <summary>
    /// Gets the bounding client rectangle for the specified element.
    /// </summary>
    /// <param name="element">The element whose bounds to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that returns the bounding rectangle.</returns>
    public async Task<DomRect> GetBoundingClientRectAsync(ElementRef element, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, element.Match()));

    /// <summary>
    /// Determines the height of the element that is currently visible within the viewport.
    /// </summary>
    /// <param name="elementRef">The element to measure.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that returns the visible height.</returns>
    public async Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<double>("getVisibleHeight", cancellationToken, elementRef.Match()));

    /// <summary>
    /// Dispatches a click event on the specified DOM element.
    /// </summary>
    /// <param name="elementRef">The element to click.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes after the event has been triggered.</returns>
    public async Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeVoidAsync("clickElement", cancellationToken, elementRef.Match()));
}