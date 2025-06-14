using Elsa.Studio.Contracts;
using MudBlazor;
using MudBlazor.Utilities;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultThemeService : IThemeService
{
    private MudTheme _currentTheme = CreateDefaultTheme();
    private bool _isDarkMode;

    /// <inheritdoc />
    public event Action? CurrentThemeChanged;

    /// <inheritdoc />
    public event Action? IsDarkModeChanged;

    /// <inheritdoc />
    public MudTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            CurrentThemeChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            IsDarkModeChanged?.Invoke();
        }
    }

    private static MudTheme CreateDefaultTheme()
    {
        var theme = new MudTheme
        {
            LayoutProperties =
            {
                DefaultBorderRadius = "4px",
            },
            PaletteLight = 
            {
                Primary = new("0ea5e9"),
                DrawerBackground = new("#f8fafc"),
                AppbarBackground = new("#0ea5e9"),
                AppbarText = new("#ffffff"),
                Background = new("#ffffff"),
                Surface = new("#f8fafc")
            },
            PaletteDark =
            {
                Primary = new("0ea5e9"),
                AppbarBackground = new("#0f172a"),
                DrawerBackground = new("#0f172a"),
                Background = new("#0f172a"),
                Surface = new("#182234"),
            }
        };

        return theme;
    }
}