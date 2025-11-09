namespace Elsa.Studio.DomInterop.Models;

/// <summary>
/// Represents the size and position of a DOM element's bounding client rectangle.
/// </summary>
public struct DomRect
{
    /// <summary>
    /// Gets or sets the X coordinate of the left edge of the rectangle relative to the viewport.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the top edge of the rectangle relative to the viewport.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the top edge of the rectangle relative to the viewport.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate of the right edge of the rectangle relative to the viewport.
    /// </summary>
    public double Right { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the bottom edge of the rectangle relative to the viewport.
    /// </summary>
    public double Bottom { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate of the left edge of the rectangle relative to the viewport.
    /// </summary>
    public double Left { get; set; }
}