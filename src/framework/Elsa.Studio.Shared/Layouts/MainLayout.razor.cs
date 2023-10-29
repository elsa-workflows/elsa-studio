using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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
    [Inject] private IFeatureService FeatureService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    private MudTheme CurrentTheme => ThemeService.CurrentTheme;
    private bool IsDarkMode => ThemeService.IsDarkMode;
    private RenderFragment UnauthorizedComponent => UnauthorizedComponentProvider.GetUnauthorizedComponent();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        ThemeService.CurrentThemeChanged += OnThemeChanged;
        AppBarService.AppBarItemsChanged += OnAppBarItemsChanged;

        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            if (authState.User.Identity?.IsAuthenticated ?? false)
                await FeatureService.InitializeFeaturesAsync();
        }

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