using Elsa.Studio.DomInterop.Models;

namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Describes utility methods that interact with DOM elements through JavaScript interop.
/// </summary>
public interface IDomAccessor
{
    /// <summary>
    /// Gets the bounding rectangle of the specified element relative to the viewport.
    /// </summary>
    /// <param name="elementRef">The element whose rectangle should be retrieved.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that returns the bounding rectangle.</returns>
    Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the visible height of an element within the viewport.
    /// </summary>
    /// <param name="elementRef">The element to inspect.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that returns the visible height.</returns>
    Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a click event on the specified element.
    /// </summary>
    /// <param name="elementRef">The element to click.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that completes when the click event has been dispatched.</returns>
    Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default);
}