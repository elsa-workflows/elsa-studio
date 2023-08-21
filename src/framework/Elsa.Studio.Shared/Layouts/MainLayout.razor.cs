using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Layouts;

/// <summary>
/// The main layout for the application.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;

    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private IAppBarService AppBarService { get; set; } = default!;
    [Inject] private IUnauthorizedComponentProvider UnauthorizedComponentProvider { get; set; } = default!;
    private MudTheme CurrentTheme => ThemeService.CurrentTheme;
    private bool IsDarkMode => ThemeService.IsDarkMode;
    private RenderFragment UnauthorizedComponent => UnauthorizedComponentProvider.GetUnauthorizedComponent();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ThemeService.CurrentThemeChanged += OnThemeChanged;
        AppBarService.AppBarItemsChanged += OnAppBarItemsChanged;

        StateHasChanged();
    }

    private void OnThemeChanged() => StateHasChanged();
    private void OnAppBarItemsChanged() => InvokeAsync(StateHasChanged);

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleDarkMode()
    {
        ThemeService.IsDarkMode = !ThemeService.IsDarkMode;
    }

    void IDisposable.Dispose()
    {
        ThemeService.CurrentThemeChanged -= OnThemeChanged;
    }
}