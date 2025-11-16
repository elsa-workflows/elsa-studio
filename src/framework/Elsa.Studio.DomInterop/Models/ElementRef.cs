using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.DomInterop.Models;

/// <summary>
/// Represents a reference to a DOM element that can be either a string selector or an ElementReference.
/// </summary>
public class ElementRef : Union<string, ElementReference>
{
    private ElementRef(string value) : base(value) { }
    private ElementRef(ElementReference value) : base(value) { }
    
    /// <summary>
    /// Implicitly converts a string to an ElementRef.
    /// </summary>
    /// <param name="value">The string selector value.</param>
    /// <returns>An ElementRef wrapping the string.</returns>
    public static implicit operator ElementRef(string value) => new(value);
    
    /// <summary>
    /// Implicitly converts an ElementReference to an ElementRef.
    /// </summary>
    /// <param name="value">The ElementReference value.</param>
    /// <returns>An ElementRef wrapping the ElementReference.</returns>
    public static implicit operator ElementRef(ElementReference value) => new(value);
}