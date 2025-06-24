using Elsa.Studio.Components.AppBar;
using Elsa.Studio.Models;

namespace Elsa.Studio.AppBarElements;

/// <summary>
/// Represents a dark mode toggle element in the application bar.
/// </summary>
/// <remarks>
/// This class is a specialized app bar element that includes functionality for enabling and disabling dark mode.
/// It inherits from <see cref="AppBarElement{DarkMode}"/> and is used to render a dark mode toggle component
/// in the application bar with a predefined order.
/// </remarks>
public class DarkModeToggleElement : AppBarElement<DarkModeToggle>
{
    /// <inheritdoc />
    public override float Order { get; set; } = 20;
}