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
                Primary = new MudColor("0ea5e9"),
                DrawerBackground = new MudColor("#f8fafc"),
                AppbarBackground = new MudColor("#0ea5e9"),
                AppbarText = new MudColor("#ffffff"),
                Background = new MudColor("#ffffff"),
                Surface = new MudColor("#f8fafc")
            },
            PaletteDark =
            {
                Primary = new MudColor("0ea5e9"),
                AppbarBackground = new MudColor("#0f172a"),
                DrawerBackground = new MudColor("#0f172a"),
                Background = new MudColor("#0f172a"),
                Surface = new MudColor("#182234"),
            }
        };

        return theme;
    }
}