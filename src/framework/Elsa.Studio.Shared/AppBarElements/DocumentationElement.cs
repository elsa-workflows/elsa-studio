using Elsa.Studio.Components.AppBar;
using Elsa.Studio.Models;

namespace Elsa.Studio.AppBarElements;

/// <summary>
/// Represents a documentation element in the application bar.
/// </summary>
public class DocumentationElement : AppBarElement<Documentation>
{
    /// <inheritdoc />
    public override float Order { get; set; } = 10;
}