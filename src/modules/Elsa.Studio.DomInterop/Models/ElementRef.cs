using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.DomInterop.Models;

public class ElementRef : Union<string, ElementReference>
{
    private ElementRef(string value) : base(value) { }
    private ElementRef(ElementReference value) : base(value) { }
    public static implicit operator ElementRef(string value) => new(value);
    public static implicit operator ElementRef(ElementReference value) => new(value);
}