using Elsa.Dashboard.Contracts;
using MudBlazor;
using MudBlazor.Utilities;

namespace Elsa.Dashboard.Services;

public class DefaultThemeService : IThemeService
{
    private MudTheme _currentTheme = CreateDefaultTheme();
    
    public event Action? CurrentThemeChanged;

    public MudTheme CurrentTheme
    {
        get => _currentTheme; 
        set 
        {
            _currentTheme = value;
            CurrentThemeChanged?.Invoke();
        }
    }
    
    private static MudTheme CreateDefaultTheme()
    {
        var theme = new MudTheme
        {
            Palette =
            {
                Primary = new MudColor("0ea5e9"),
                DrawerBackground = new MudColor("#f8fafc"),
                AppbarBackground = new MudColor("#ffffff"),
                Background = new MudColor("#ffffff"),
                Surface = new MudColor("#f8fafc")
            },
            PaletteDark =
            {
                Primary = new MudColor("0ea5e9"),
                AppbarBackground = new MudColor("#0f172a"),
                DrawerBackground = new MudColor("#0f172a"),
                Background = new MudColor("#0f172a"),
                Surface = new MudColor("#182234")
            }
        };

        return theme;
    }
}