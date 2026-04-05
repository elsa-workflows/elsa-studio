using Elsa.Studio.Contracts;
using MudBlazor;
using MudBlazor.Utilities;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides the default theme for the dashboard.
/// </summary>
public class DefaultThemeProvider : IThemeProvider
{
    /// <inheritdoc />
    public MudTheme GetTheme()
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
