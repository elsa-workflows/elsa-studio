using Elsa.Studio.DomInterop.Models;

namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Provides access to the browser's DOM.
/// </summary>
public interface IDomAccessor
{
    Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default);
    Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default);
    Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default);
    
    // Shadow DOM support
    Task CreateShadowRootAsync(ElementRef elementRef, string mode = "open", CancellationToken cancellationToken = default);
    Task InjectStylesheetsAsync(ElementRef elementRef, string[] stylesheets, CancellationToken cancellationToken = default);
    Task SetupElsaShadowRootAsync(ElementRef elementRef, string[]? customStylesheets = null, CancellationToken cancellationToken = default);
    Task CreateElsaCustomElementAsync(string tagName, string componentName, bool enableShadowDOM = false, CancellationToken cancellationToken = default);
}