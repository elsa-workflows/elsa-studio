using MudBlazor;

namespace Elsa.Dashboard.Contracts;

/// <summary>
/// Provides theme information to the dashboard.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Raised when the current theme changes.
    /// </summary>
    event Action CurrentThemeChanged;

    /// <summary>
    /// The current theme.
    /// </summary>
    MudTheme CurrentTheme { get; set; }
}